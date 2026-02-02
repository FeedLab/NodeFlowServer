namespace NodeFlow.Server.Contracts.Auth.Response;

public sealed record CreateUserResponse(
    Guid Id,
    string UserName,
    string Email,
    DateTimeOffset CreatedAtUtc,
    string Location);
