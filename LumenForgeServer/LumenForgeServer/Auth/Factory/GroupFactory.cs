using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Dto.Command;
using NodaTime;

namespace LumenForgeServer.Auth.Factory;

/// <summary>
/// Factory methods for constructing group domain entities from DTO payloads.
/// </summary>
public static class GroupFactory
{
    public static Group BuildGroup(AddGroupDto dto)
    {
        var dateNow = SystemClock.Instance.GetCurrentInstant();
        return new Group
        {
            Guid = Guid.CreateVersion7(),
            CreatedAt = dateNow,
            UpdatedAt = dateNow,
            Name = dto.Name,
            Description = dto.Description,
        };
    }
}
