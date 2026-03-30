using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Dto.Command;
using NodaTime;

namespace LumenForgeServer.Auth.Factory;

/// <summary>
/// Factory methods for constructing group domain entities from DTO payloads.
/// </summary>
public static class GroupFactory
{
    /// <summary>
    /// Executes the build group operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="dto">Request payload containing the input data required for the operation.</param>
    /// <returns>The operation result.</returns>
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
