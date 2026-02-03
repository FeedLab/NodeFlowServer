using Microsoft.EntityFrameworkCore;
using NodeFlow.Server.Data.Mapping;
using NodeFlow.Server.Domain.Models;
using NodeFlow.Server.Domain.Repositories;

namespace NodeFlow.Server.Data.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly NodeFlowDbContext dbContext;

    public UserRepository(NodeFlowDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Users.FirstOrDefaultAsync(
            user => user.Id == id,
            cancellationToken);

        return entity?.ToModel();
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Users.FirstOrDefaultAsync(
            user => user.Email == email,
            cancellationToken);

        return entity?.ToModel();
    }

    public async Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Users.FirstOrDefaultAsync(
            user => user.UserName == userName,
            cancellationToken);

        return entity?.ToModel();
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken)
    {
        var entity = user.ToEntity();
        dbContext.Users.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        var entity = user.ToEntity();
        dbContext.Users.Update(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
