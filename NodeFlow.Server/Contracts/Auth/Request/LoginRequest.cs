namespace NodeFlow.Server.Contracts.Auth.Request;

public sealed record LoginRequest(string Identifier, string Password);
