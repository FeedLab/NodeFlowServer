namespace NodeFlow.Server.Domain.Models;

/// <summary>
/// Domain model representing a user session
/// </summary>
public sealed class Session
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public string RefreshToken { get; init; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? LastAccessedAtUtc { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public User? User { get; init; }
}
