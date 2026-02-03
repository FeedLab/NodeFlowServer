using NodeFlow.Server.Data.Entities;
using NodeFlow.Server.Domain.Models;

namespace NodeFlow.Server.Data.Mapping;

internal static class UserMappingExtensions
{
    public static Domain.Models.User ToModel(this Entities.User entity)
    {
        return new Domain.Models.User
        {
            Id = entity.Id,
            UserName = entity.UserName,
            Email = entity.Email,
            PasswordHash = entity.PasswordHash,
            CreatedAtUtc = entity.CreatedAtUtc
        };
    }

    public static Entities.User ToEntity(this Domain.Models.User model)
    {
        return new Entities.User
        {
            Id = model.Id,
            UserName = model.UserName,
            Email = model.Email,
            PasswordHash = model.PasswordHash,
            CreatedAtUtc = model.CreatedAtUtc
        };
    }
}
