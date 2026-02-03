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
    private static async Task<IResult> RefreshTokenAsync(
        RefreshTokenRequest request,
        ISessionRepository sessionRepository,
        IOptions<JwtOptions> jwtOptions,
        CancellationToken cancellationToken)
    {
        var refreshToken = request.RefreshToken.Trim();

        var session = await sessionRepository.GetByRefreshTokenAsync(refreshToken, cancellationToken);

        if (session == null)
        {
            return Results.Unauthorized();
        }

        // Check if refresh token is expired
        if (session.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return Results.Unauthorized();
        }

        var user = session.User!;

        // Generate new access token
        var options = jwtOptions.Value;
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

        // Generate new refresh token
        var newRefreshToken = GenerateRefreshToken();
        var refreshTokenExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(options.RefreshTokenMinutes);
        var updatedSession = new Session
        {
            Id = session.Id,
            UserId = session.UserId,
            RefreshToken = newRefreshToken,
            ExpiresAtUtc = refreshTokenExpiresAtUtc,
            CreatedAtUtc = session.CreatedAtUtc,
            LastAccessedAtUtc = DateTimeOffset.UtcNow,
            IpAddress = session.IpAddress,
            UserAgent = session.UserAgent
        };
        await sessionRepository.UpdateAsync(updatedSession, cancellationToken);

        var response = new LoginResponse(tokenValue, expiresAtUtc, "Bearer", newRefreshToken, refreshTokenExpiresAtUtc);
        return Results.Ok(response);
    }
}
