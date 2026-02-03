namespace NodeFlow.Server.Data.Entities;

public sealed class UserProfile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string? DisplayName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Bio { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public User User { get; set; } = null!;
}
