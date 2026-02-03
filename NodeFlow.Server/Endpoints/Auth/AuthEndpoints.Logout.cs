using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using NodeFlow.Server.Domain.Repositories;

namespace NodeFlow.Server.Endpoints.Auth;

public static partial class AuthEndpoints
{
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
}
