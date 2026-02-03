using NodeFlow.Server.Contracts.Auth.Request;
using NodeFlow.Server.Contracts.Auth.Response;
using NodeFlow.Server.Domain.Models;
using NodeFlow.Server.Domain.Repositories;

namespace NodeFlow.Server.Endpoints.Auth;

public static partial class AuthEndpoints
{
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
}
