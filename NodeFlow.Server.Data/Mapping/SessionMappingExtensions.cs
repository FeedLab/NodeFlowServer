using NodeFlow.Server.Data.Entities;
using NodeFlow.Server.Domain.Models;

namespace NodeFlow.Server.Data.Mapping;

internal static class SessionMappingExtensions
{
    public static Domain.Models.Session ToModel(this Entities.Session entity)
    {
        return new Domain.Models.Session
        {
            Id = entity.Id,
            UserId = entity.UserId,
            RefreshToken = entity.RefreshToken,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            CreatedAtUtc = entity.CreatedAtUtc,
            LastAccessedAtUtc = entity.LastAccessedAtUtc,
            IpAddress = entity.IpAddress,
            UserAgent = entity.UserAgent,
            User = entity.User?.ToModel()
        };
    }

    public static Entities.Session ToEntity(this Domain.Models.Session model)
    {
        return new Entities.Session
        {
            Id = model.Id,
            UserId = model.UserId,
            RefreshToken = model.RefreshToken,
            ExpiresAtUtc = model.ExpiresAtUtc,
            CreatedAtUtc = model.CreatedAtUtc,
            LastAccessedAtUtc = model.LastAccessedAtUtc,
            IpAddress = model.IpAddress,
            UserAgent = model.UserAgent
        };
    }
}
