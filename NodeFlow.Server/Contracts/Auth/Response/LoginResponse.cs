namespace NodeFlow.Server.Contracts.Auth.Response;

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string TokenType,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);
