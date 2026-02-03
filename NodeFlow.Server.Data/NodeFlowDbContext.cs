using Microsoft.EntityFrameworkCore;
using NodeFlow.Server.Data.Entities;

namespace NodeFlow.Server.Data;

public sealed class NodeFlowDbContext : DbContext
{
    public NodeFlowDbContext(DbContextOptions<NodeFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.UserName).HasMaxLength(64).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(256).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(user => user.CreatedAtUtc).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.HasIndex(user => user.UserName).IsUnique();

            entity.HasOne(user => user.Profile)
                .WithOne(profile => profile.User)
                .HasForeignKey<UserProfile>(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(user => user.Sessions)
                .WithOne(session => session.User)
                .HasForeignKey(session => session.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(session => session.Id);
            entity.Property(session => session.UserId).IsRequired();
            entity.Property(session => session.RefreshToken).HasMaxLength(512).IsRequired();
            entity.Property(session => session.ExpiresAtUtc).IsRequired();
            entity.Property(session => session.CreatedAtUtc).IsRequired();
            entity.Property(session => session.LastAccessedAtUtc);
            entity.Property(session => session.IpAddress).HasMaxLength(45);
            entity.Property(session => session.UserAgent).HasMaxLength(512);

            // Indexes
            entity.HasIndex(session => session.RefreshToken).IsUnique();
            entity.HasIndex(session => session.UserId);
            entity.HasIndex(session => session.ExpiresAtUtc); // For cleanup of expired sessions
            entity.HasIndex(session => new { session.UserId, session.ExpiresAtUtc }); // Composite for active user sessions
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(profile => profile.Id);
            entity.Property(profile => profile.UserId).IsRequired();
            entity.Property(profile => profile.DisplayName).HasMaxLength(128);
            entity.Property(profile => profile.FirstName).HasMaxLength(64);
            entity.Property(profile => profile.LastName).HasMaxLength(64);
            entity.Property(profile => profile.AvatarUrl).HasMaxLength(512);
            entity.Property(profile => profile.Bio).HasMaxLength(1000);
            entity.Property(profile => profile.UpdatedAtUtc).IsRequired();
            entity.HasIndex(profile => profile.UserId).IsUnique();
        });
    }
}
