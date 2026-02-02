namespace NodeFlow.Server.Contracts.Auth.Request;

public sealed record CreateUserRequest(string UserName, string Email, string Password);
