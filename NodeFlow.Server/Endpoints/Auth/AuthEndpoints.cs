using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NodeFlow.Server.Contracts.Auth.Request;
using NodeFlow.Server.Contracts.Auth.Response;
using NodeFlow.Server.Domain.Models;
using NodeFlow.Server.Domain.Repositories;
using NodeFlow.Server.Endpoints.Filters;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NodeFlow.Server.Endpoints.Auth;

public static class AuthEndpoints
{
    public static IEndpointConventionBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth");

        group.MapPost("/users", CreateUserAsync)
            .WithRequestValidation<CreateUserRequest>()
            .WithName("CreateUser");

        group.MapPost("/login", LoginAsync)
            .WithRequestValidation<LoginRequest>()
            .WithName("Login");

        group.MapPost("/refresh", RefreshTokenAsync)
            .WithRequestValidation<RefreshTokenRequest>()
            .RequireAuthorization()
            .WithName("RefreshToken");

        return group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization()
            .WithName("Logout");
    }

    private static async Task<IResult> CreateUserAsync(
        CreateUserRequest request,
        IUserRepository repository,
        CancellationToken cancellationToken)
    {
        var userName = request.UserName.Trim();
        var email = request.Email.Trim();
        var password = request.Password;

        var existingByEmail = await repository.GetByEmailAsync(email, cancellationToken);
        if (existingByEmail is not null)
        {
            return Results.Conflict("Email already exists.");
        }

        var existingByUserName = await repository.GetByUserNameAsync(userName, cancellationToken);
        if (existingByUserName is not null)
        {
            return Results.Conflict("UserName already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            PasswordHash = HashPassword(password),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var created = await repository.CreateAsync(user, cancellationToken);

        var location = $"/auth/users/{created.Id}";
        var response = new CreateUserResponse(created.Id, created.UserName, created.Email, created.CreatedAtUtc, location);
        return Results.Json(response, statusCode: StatusCodes.Status201Created);
    }

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

    private static async Task<IResult> LogoutAsync(
        ClaimsPrincipal user,
        ISessionRepository sessionRepository,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("[DEBUG_LOG] LogoutAsync started");
        if (user.Identity?.IsAuthenticated == true)
        {
            Console.WriteLine($"[DEBUG_LOG] User is authenticated: {user.Identity.Name}");
        }
        else
        {
            Console.WriteLine("[DEBUG_LOG] User is NOT authenticated");
        }

        foreach (var claim in user.Claims)
        {
            Console.WriteLine($"[DEBUG_LOG] Claim: {claim.Type} = {claim.Value}");
        }

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst(JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            Console.WriteLine("[DEBUG_LOG] UserId claim not found or invalid");
            return Results.Unauthorized();
        }

        // Delete all sessions for this user
        await sessionRepository.DeleteAllByUserIdAsync(userId, cancellationToken);

        return Results.Ok();
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static string HashPassword(string password)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(hash);
    }
}
