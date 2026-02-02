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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.UserName).HasMaxLength(64).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(256).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(user => user.CreatedAtUtc).IsRequired();
            entity.Property(user => user.LastLoginUtc);
            entity.HasIndex(user => user.Email).IsUnique();
            entity.HasIndex(user => user.UserName).IsUnique();
        });
    }
}
