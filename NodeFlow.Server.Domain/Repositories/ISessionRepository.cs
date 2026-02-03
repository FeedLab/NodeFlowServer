using NodeFlow.Server.Domain.Models;

namespace NodeFlow.Server.Domain.Repositories;

public interface ISessionRepository
{
    Task<Session?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);

    Task<Session> CreateAsync(Session session, CancellationToken cancellationToken);

    Task UpdateAsync(Session session, CancellationToken cancellationToken);

    Task DeleteAsync(Session session, CancellationToken cancellationToken);

    Task DeleteAllByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
