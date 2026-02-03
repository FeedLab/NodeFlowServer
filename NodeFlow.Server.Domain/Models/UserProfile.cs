namespace NodeFlow.Server.Domain.Models;

/// <summary>
/// Domain model representing user profile/domain data
/// </summary>
public sealed class UserProfile
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public string? DisplayName { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? AvatarUrl { get; init; }

    public string? Bio { get; init; }

    public DateTimeOffset UpdatedAtUtc { get; init; }
}
