using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Inventory.Domain;
using LumenForgeServer.Inventory.Dto.Create;
using LumenForgeServer.Inventory.Dto.View;
using LumenForgeServer.Inventory.Persistance;
using NodaTime;

namespace LumenForgeServer.Inventory.Service;

public class DeviceRelationService(IInventoryRepository repository)
{
    /// <summary>
    /// Executes the create relation operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="dto">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the DeviceRelationView result.</returns>
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

        var parentDevice = await repository.GetDeviceByGuidAsync(dto.ParentDeviceGuid, ct)
            ?? throw new NotFoundException($"Parent device not found.");

        if (dto.ContainedAmount > parentDevice.StockAmount)
        {
            throw new ValidationException("Validation failed.", new Dictionary<string, string[]>
            {
                [nameof(dto.ContainedAmount)] = [$"Parent device only has {parentDevice.StockAmount} units in stock, cannot contain {dto.ContainedAmount} units."]
            });
        }

        var existing = await repository.GetActiveDeviceRelationAsync(parentId, childId, ct);
        if (existing is not null)
        {
            throw new ValidationException("Validation failed.", new Dictionary<string, string[]>
            {
                ["DeviceRelation"] = ["A relation between those devices already exists."]
            });
        }

        var existingRelations = await repository.GetDeviceRelationsByParentDeviceIdAsync(parentId, ct);
        var totalContainedAmount = existingRelations.Sum(r => r.ContainedAmount) + dto.ContainedAmount;

        if (totalContainedAmount > parentDevice.StockAmount)
        {
            throw new ValidationException("Validation failed.", new Dictionary<string, string[]>
            {
                [nameof(dto.ContainedAmount)] = [$"Total contained amount ({totalContainedAmount}) exceeds parent device stock ({parentDevice.StockAmount}). Current relations contain {existingRelations.Sum(r => r.ContainedAmount)} units."]
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

    /// <summary>
    /// Executes the list relations for parent operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="parentDeviceGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the IReadOnlyList&lt;DeviceRelationView&gt; result.</returns>
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

    /// <summary>
    /// Executes the delete relation operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="relationGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Executes the has path async operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="startDeviceId">Numeric input used by this operation.</param>
    /// <param name="targetDeviceId">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the bool result.</returns>
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
