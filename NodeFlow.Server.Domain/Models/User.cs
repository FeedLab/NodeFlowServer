namespace NodeFlow.Server.Domain.Models;

/// <summary>
/// Domain model representing a user for authentication
/// </summary>
public sealed class User
{
    public Guid Id { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string PasswordHash { get; init; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; init; }
}
