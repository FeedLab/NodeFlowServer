using NodeFlow.Server.Data.Entities;

namespace NodeFlow.Server.Data.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken);

    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);

    Task<User> CreateAsync(User user, CancellationToken cancellationToken);

    Task UpdateAsync(User user, CancellationToken cancellationToken);
}
