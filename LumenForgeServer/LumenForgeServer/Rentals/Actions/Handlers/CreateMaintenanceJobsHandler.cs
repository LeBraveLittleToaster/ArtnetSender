using LumenForgeServer.Common.Database;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Maintenance.Domain;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LumenForgeServer.Rentals.Actions.Handlers;

/// <summary>Input for the <see cref="CreateMaintenanceJobsHandler"/>.</summary>
public sealed class CreateMaintenanceJobsInput : ActionInput
{
    /// <summary>Stock binding GUIDs of damaged items that need maintenance.</summary>
    public required List<Guid> DamagedStockBindingGuids { get; init; }
}

/// <summary>
/// Creates maintenance jobs in the Maintenance module for items that were
/// found damaged during inspection. Internal cross-module action.
/// </summary>
public sealed class CreateMaintenanceJobsHandler(
    IRentalProcessRepository processRepository,
    AppDbContext db)
    : RentalActionHandlerBase<CreateMaintenanceJobsInput>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.CreateMaintenanceJobs;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.Returned, RentalStage.Inspected };

    /// <inheritdoc />
    protected override Task<ActionResult> BeforeExecuteAsync(
        RentalProcessInstance process, CreateMaintenanceJobsInput input, CancellationToken ct)
    {
        if (input.DamagedStockBindingGuids.Count == 0)
            return Task.FromResult(ActionResult.Fail(nameof(RentalActionType.CreateMaintenanceJobs), "DamagedStockBindingGuids",
                "At least one damaged stock binding GUID is required."));

        return Task.FromResult(ActionResult.Ok(nameof(RentalActionType.CreateMaintenanceJobs)));
    }

    /// <inheritdoc />
    protected override async Task<ActionResult> ExecuteAsync(
        RentalProcessInstance process, CreateMaintenanceJobsInput input, CancellationToken ct)
    {
        var now = SystemClock.Instance.GetCurrentInstant();

        var bindings = await db.StockBindings
            .Include(sb => sb.Device)
            .Where(sb => input.DamagedStockBindingGuids.Contains(sb.Guid))
            .ToListAsync(ct);

        if (bindings.Count == 0)
            return ActionResult.Fail(nameof(RentalActionType.CreateMaintenanceJobs), "DamagedStockBindingGuids",
                "No matching stock bindings found.");

        foreach (var binding in bindings)
        {
            var job = new MaintenanceJob
            {
                Guid = Guid.NewGuid(),
                Name = $"Rental damage — {binding.Device?.DeviceName ?? binding.Device?.SerialNumber ?? binding.Guid.ToString()}",
                Description = $"Maintenance job created from rental process {process.Guid} for damaged item.",
                Status = MaintenanceStatus.Reported,
                CreatedByUserKcId = input.ActorKcId,
                RelatedToRentalId = process.RentalId,
                AffectedDevices = binding.Device is not null ? [binding.Device] : [],
                ReportedAt = now,
                UpdatedAt = now
            };

            await db.MaintenanceJobs.AddAsync(job, ct);
        }

        return ActionResult.Ok(nameof(RentalActionType.CreateMaintenanceJobs));
    }
}
