using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Inventory.Domain;
using LumenForgeServer.Inventory.Dto.Create;
using LumenForgeServer.Inventory.Dto.View;
using LumenForgeServer.Inventory.Persistance;
using NodaTime;

namespace LumenForgeServer.Inventory.Service;

public class DeviceRelationService(IInventoryRepository repository)
{
    public async Task<DeviceRelationView> CreateRelation(CreateDeviceRelationDto dto, CancellationToken ct)
    {
        if (dto.ParentDeviceGuid == Guid.Empty || dto.ChildDeviceGuid == Guid.Empty)
        {
            throw new ValidationException("Validation failed.", new Dictionary<string, string[]>
            {
                ["DeviceGuid"] = ["Parent and child device GUID must not be empty."]
            });
        }

        if (dto.ParentDeviceGuid == dto.ChildDeviceGuid)
        {
            throw new ValidationException("Validation failed.", new Dictionary<string, string[]>
            {
                ["DeviceRelation"] = ["A device cannot contain itself."]
            });
        }

        if (dto.ContainedAmount <= 0)
        {
            throw new ValidationException("Validation failed.", new Dictionary<string, string[]>
            {
                [nameof(dto.ContainedAmount)] = ["Contained amount must be greater than zero."]
            });
        }

        var parentId = await repository.TryGetDeviceIdByGuidAsync(dto.ParentDeviceGuid, ct)
            ?? throw new NotFoundException($"Parent device '{dto.ParentDeviceGuid}' not found.");

        var childId = await repository.TryGetDeviceIdByGuidAsync(dto.ChildDeviceGuid, ct)
            ?? throw new NotFoundException($"Child device '{dto.ChildDeviceGuid}' not found.");

        var existing = await repository.GetActiveDeviceRelationAsync(parentId, childId, ct);
        if (existing is not null)
        {
            throw new ValidationException("Validation failed.", new Dictionary<string, string[]>
            {
                ["DeviceRelation"] = ["A relation between those devices already exists."]
            });
        }

        var createsCycle = await HasPathAsync(childId, parentId, ct);
        if (createsCycle)
        {
            throw new ValidationException("Validation failed.", new Dictionary<string, string[]>
            {
                ["DeviceRelation"] = ["Relation would create a cycle."]
            });
        }

        var now = SystemClock.Instance.GetCurrentInstant();
        var relation = new DeviceRelation
        {
            Guid = Guid.CreateVersion7(),
            ParentDeviceId = parentId,
            ChildDeviceId = childId,
            ContainedAmount = dto.ContainedAmount,
            RelationType = dto.RelationType,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddDeviceRelationAsync(relation, ct);
        await repository.AddDeviceRelationAuditLogAsync(new DeviceRelationAuditLog
        {
            Guid = Guid.CreateVersion7(),
            RelationGuid = relation.Guid,
            ParentDeviceId = relation.ParentDeviceId,
            ChildDeviceId = relation.ChildDeviceId,
            ContainedAmount = relation.ContainedAmount,
            RelationType = relation.RelationType,
            Action = DeviceRelationAuditAction.Created,
            OccurredAt = now
        }, ct);
        await repository.SaveChangesAsync(ct);

        var created = await repository.GetDeviceRelationByGuidAsync(relation.Guid, ct)
            ?? throw new NotFoundException("Device relation not found after creation.");

        return DeviceRelationView.FromEntity(created);
    }

    public async Task<IReadOnlyList<DeviceRelationView>> ListRelationsForParent(Guid parentDeviceGuid, CancellationToken ct)
    {
        if (parentDeviceGuid == Guid.Empty)
        {
            throw new ValidationException("Validation failed.", new Dictionary<string, string[]>
            {
                [nameof(parentDeviceGuid)] = ["GUID must not be empty."]
            });
        }

        var parentId = await repository.TryGetDeviceIdByGuidAsync(parentDeviceGuid, ct)
            ?? throw new NotFoundException($"Device '{parentDeviceGuid}' not found.");

        var relations = await repository.GetDeviceRelationsByParentDeviceIdAsync(parentId, ct);
        return relations.Select(DeviceRelationView.FromEntity).ToList();
    }

    public async Task DeleteRelation(Guid relationGuid, CancellationToken ct)
    {
        var relation = await repository.GetDeviceRelationByGuidAsync(relationGuid, ct)
            ?? throw new NotFoundException($"Device relation '{relationGuid}' not found.");

        var now = SystemClock.Instance.GetCurrentInstant();
        await repository.AddDeviceRelationAuditLogAsync(new DeviceRelationAuditLog
        {
            Guid = Guid.CreateVersion7(),
            RelationGuid = relation.Guid,
            ParentDeviceId = relation.ParentDeviceId,
            ChildDeviceId = relation.ChildDeviceId,
            ContainedAmount = relation.ContainedAmount,
            RelationType = relation.RelationType,
            Action = DeviceRelationAuditAction.Deleted,
            OccurredAt = now
        }, ct);

        await repository.DeleteDeviceRelationAsync(relation, ct);
        await repository.SaveChangesAsync(ct);
    }

    private async Task<bool> HasPathAsync(long startDeviceId, long targetDeviceId, CancellationToken ct)
    {
        var visited = new HashSet<long>();
        var queue = new Queue<long>();
        queue.Enqueue(startDeviceId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }

            if (current == targetDeviceId)
            {
                return true;
            }

            var relations = await repository.GetDeviceRelationsByParentDeviceIdAsync(current, ct);
            foreach (var relation in relations)
            {
                queue.Enqueue(relation.ChildDeviceId);
            }
        }

        return false;
    }
}
