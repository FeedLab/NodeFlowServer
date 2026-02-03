using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NodeFlow.Server.Contracts.Auth.Request;
using NodeFlow.Server.Contracts.Auth.Response;
using NodeFlow.Server.Domain.Models;
using NodeFlow.Server.Domain.Repositories;

namespace NodeFlow.Server.Endpoints.Auth;

public static partial class AuthEndpoints
{
    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IUserRepository userRepository,
        ISessionRepository sessionRepository,
        IOptions<JwtOptions> jwtOptions,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("[DEBUG_LOG] LoginAsync started");
        var identifier = request.Identifier.Trim();
        var password = request.Password;

        var user = await userRepository.GetByEmailAsync(identifier, cancellationToken);
        if (user is null)
        {
            user = await userRepository.GetByUserNameAsync(identifier, cancellationToken);
        }

        if (user is null)
        {
            Console.WriteLine("[DEBUG_LOG] User not found");
            return Results.Unauthorized();
        }

        var passwordHash = HashPassword(password);
        if (!string.Equals(user.PasswordHash, passwordHash, StringComparison.Ordinal))
        {
            Console.WriteLine("[DEBUG_LOG] Invalid password");
            return Results.Unauthorized();
        }

        var options = jwtOptions.Value;
        Console.WriteLine($"[DEBUG_LOG] JwtOptions: Issuer={options.Issuer}, Audience={options.Audience}, SigningKey={options.SigningKey}");
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(options.AccessTokenMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);
        Console.WriteLine($"[DEBUG_LOG] Generated Token: {tokenValue}");

        // Create new session
        var refreshToken = GenerateRefreshToken();
        var refreshTokenExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(options.RefreshTokenMinutes);
        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RefreshToken = refreshToken,
            ExpiresAtUtc = refreshTokenExpiresAtUtc,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastAccessedAtUtc = DateTimeOffset.UtcNow
        };
        await sessionRepository.CreateAsync(session, cancellationToken);

        var response = new LoginResponse(tokenValue, expiresAtUtc, "Bearer", refreshToken, refreshTokenExpiresAtUtc);
        return Results.Ok(response);
    }
}
