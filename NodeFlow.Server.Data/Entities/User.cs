namespace NodeFlow.Server.Data.Entities;

public sealed class User
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public UserProfile? Profile { get; set; }

    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
