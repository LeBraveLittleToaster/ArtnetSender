using LumenForgeServer.Common.Database;
using LumenForgeServer.Maintenance.Domain;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="CreateMaintenanceJobsHandler"/>.</summary>
public sealed class CreateMaintenanceJobsInput : ActionInput
{
    /// <summary>Stock binding GUIDs of damaged items that need maintenance.</summary>
    public required List<Guid> DamagedStockBindingGuids { get; init; }
}

/// <summary>Extended result carrying the GUIDs of created maintenance jobs.</summary>
public sealed class CreateMaintenanceJobsResult : ActionResult
{
    /// <summary>GUIDs of the newly created maintenance jobs.</summary>
    public Guid[] MaintenanceJobGuids { get; init; } = [];
}

/// <summary>
/// Creates maintenance jobs in the Maintenance module for items that were
/// found damaged during inspection. Internal cross-module action.
/// Returns the GUIDs of all created maintenance jobs.
/// </summary>
public sealed class CreateMaintenanceJobsHandler(
    IRentalProcessRepository processRepository,
    AppDbContext db)
    : RentalActionHandlerBase<CreateMaintenanceJobsInput, CreateMaintenanceJobsResult>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.CreateMaintenanceJobs;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.Returned, RentalStage.Inspected };

    protected override Task AfterExecuteAsync(RentalProcessInstance process, CreateMaintenanceJobsResult result, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task<BlankActionResult> BeforeExecuteAsync(
        RentalProcessInstance process, CreateMaintenanceJobsInput input, CancellationToken ct)
    {
        if (input.DamagedStockBindingGuids.Count == 0)
            return new BlankActionResult
            {
                Success = false,
                ActionName = nameof(RentalActionType.CreateMaintenanceJobs),
                Errors = new() { ["DamagedStockBindingGuids"] = "At least one damaged stock binding GUID is required." }
            };

        return new BlankActionResult
        {
            Success = true,
            ActionName = this.ActionType.ToString(),
        };
    }

    /// <inheritdoc />
    protected override async Task<CreateMaintenanceJobsResult> ExecuteAsync(
        RentalProcessInstance process, CreateMaintenanceJobsInput input, CancellationToken ct)
    {
        var now = SystemClock.Instance.GetCurrentInstant();

        var bindings = await db.StockBindings
            .Include(sb => sb.Device)
            .Where(sb => input.DamagedStockBindingGuids.Contains(sb.Guid))
            .ToListAsync(ct);

        if (bindings.Count == 0)
            return new CreateMaintenanceJobsResult
            {
                Success = false,
                ActionName = nameof(RentalActionType.CreateMaintenanceJobs),
                MaintenanceJobGuids = [],
                Errors = new() { ["DamagedStockBindingGuids"] = "No matching stock bindings found." }
            };

        var createdJobGuids = new List<Guid>();

        foreach (var binding in bindings)
        {
            var jobGuid = Guid.NewGuid();

            var job = new MaintenanceJob
            {
                Guid = jobGuid,
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
            createdJobGuids.Add(jobGuid);
        }

        return new CreateMaintenanceJobsResult
        {
            Success = true,
            ActionName = nameof(RentalActionType.CreateMaintenanceJobs),
            MaintenanceJobGuids = createdJobGuids.ToArray()
        };
    }
}
