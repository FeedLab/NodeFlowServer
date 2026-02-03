namespace NodeFlow.Server.Data.Entities;

public sealed class Session
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string RefreshToken { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? LastAccessedAtUtc { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public User User { get; set; } = null!;
}
