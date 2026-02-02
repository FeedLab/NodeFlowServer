using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NodeFlow.Server.Contracts.Auth.Request;
using NodeFlow.Server.Contracts.Auth.Response;
using NodeFlow.Server.Data.Entities;
using NodeFlow.Server.Data.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NodeFlow.Server.Endpoints.Auth;

public static class AuthEndpoints
{
    public static IEndpointConventionBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth");

        group.MapPost("/users", CreateUserAsync)
            .WithName("CreateUser");

        return group.MapPost("/login", LoginAsync)
            .WithName("Login");
    }

    private static async Task<IResult> CreateUserAsync(
        CreateUserRequest request,
        IUserRepository repository,
        CancellationToken cancellationToken)
    {
        var userName = request.UserName?.Trim();
        var email = request.Email?.Trim();
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return Results.BadRequest("UserName, Email, and Password are required.");
        }

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
        IUserRepository repository,
        IOptions<JwtOptions> jwtOptions,
        CancellationToken cancellationToken)
    {
        var identifier = request.Identifier?.Trim();
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
        {
            return Results.BadRequest("Identifier and Password are required.");
        }

        var user = await repository.GetByEmailAsync(identifier, cancellationToken);
        if (user is null)
        {
            user = await repository.GetByUserNameAsync(identifier, cancellationToken);
        }

        if (user is null)
        {
            return Results.Unauthorized();
        }

        var passwordHash = HashPassword(password);
        if (!string.Equals(user.PasswordHash, passwordHash, StringComparison.Ordinal))
        {
            return Results.Unauthorized();
        }

        user.LastLoginUtc = DateTimeOffset.UtcNow;
        await repository.UpdateAsync(user, cancellationToken);

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
        var response = new LoginResponse(tokenValue, expiresAtUtc, "Bearer");
        return Results.Ok(response);
    }

    private static string HashPassword(string password)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(hash);
    }
}
