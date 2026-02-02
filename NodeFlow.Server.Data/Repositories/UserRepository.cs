using Microsoft.EntityFrameworkCore;
using NodeFlow.Server.Data.Entities;

namespace NodeFlow.Server.Data.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly NodeFlowDbContext dbContext;

    public UserRepository(NodeFlowDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return dbContext.Users.FirstOrDefaultAsync(
            user => user.Email == email,
            cancellationToken);
    }

    public Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken)
    {
        return dbContext.Users.FirstOrDefaultAsync(
            user => user.UserName == userName,
            cancellationToken);
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken)
    {
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
