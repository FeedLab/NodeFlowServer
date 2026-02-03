using NodeFlow.Server.Contracts.Auth.Request;
using NodeFlow.Server.Endpoints.Filters;

namespace NodeFlow.Server.Endpoints.Auth;

public static partial class AuthEndpoints
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
            .WithName("RefreshToken");

        return group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization()
            .WithName("Logout");
    }
}
