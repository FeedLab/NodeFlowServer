using Microsoft.EntityFrameworkCore;
using NodeFlow.Server.Data.Mapping;
using NodeFlow.Server.Domain.Models;
using NodeFlow.Server.Domain.Repositories;

namespace NodeFlow.Server.Data.Repositories;

public sealed class SessionRepository : ISessionRepository
{
    private readonly NodeFlowDbContext dbContext;

    public SessionRepository(NodeFlowDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<Session?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Sessions
            .Include(session => session.User)
            .FirstOrDefaultAsync(
                session => session.RefreshToken == refreshToken,
                cancellationToken);

        return entity?.ToModel();
    }

    public async Task<Session> CreateAsync(Session session, CancellationToken cancellationToken)
    {
        var entity = session.ToEntity();
        dbContext.Sessions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    public async Task UpdateAsync(Session session, CancellationToken cancellationToken)
    {
        // Find and update the tracked entity
        var trackedEntity = await dbContext.Sessions.FindAsync([session.Id], cancellationToken);
        if (trackedEntity != null)
        {
            trackedEntity.RefreshToken = session.RefreshToken;
            trackedEntity.ExpiresAtUtc = session.ExpiresAtUtc;
            trackedEntity.LastAccessedAtUtc = session.LastAccessedAtUtc;
            trackedEntity.IpAddress = session.IpAddress;
            trackedEntity.UserAgent = session.UserAgent;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(Session session, CancellationToken cancellationToken)
    {
        var entity = session.ToEntity();
        dbContext.Sessions.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAllByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var sessions = await dbContext.Sessions
            .Where(session => session.UserId == userId)
            .ToListAsync(cancellationToken);

        dbContext.Sessions.RemoveRange(sessions);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
