using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Persistence;
using NodaTime;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="CreateRentalHandler"/>.</summary>
public sealed class CreateRentalInput : ActionInput
{
    /// <summary>Display name of the customer.</summary>
    public string? CustomerName { get; init; }

    /// <summary>Contact email for rental communication.</summary>
    public string? CustomerEmail { get; init; }

    /// <summary>Free-text description of the rental purpose.</summary>
    public string? Purpose { get; init; }

    /// <summary>Requested start of the rental period.</summary>
    public required Instant RequestedStart { get; init; }

    /// <summary>Requested end of the rental period.</summary>
    public required Instant RequestedEnd { get; init; }

    /// <summary>Optional notes from the customer.</summary>
    public string? Notes { get; init; }
}

/// <summary>Extended result that carries the newly created process GUID back to the caller.</summary>
public sealed class CreateRentalResult : ActionResult
{
    /// <summary>GUID of the newly created <see cref="RentalProcessInstance"/>.</summary>
    public Guid ProcessInstanceGuid { get; init; } = Guid.Empty;
}

/// <summary>
/// Creates a new <see cref="RentalProcessInstance"/> and its backing
/// <see cref="Rental"/> data aggregate. This is the entry point of every rental
/// workflow — after execution the process is in <see cref="RentalStage.Requested"/>.
/// </summary>
public sealed class CreateRentalHandler(IRentalProcessRepository repository)
    : RentalActionHandlerBase<CreateRentalInput, CreateRentalResult>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.CreateRental;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.None };

    protected override async Task AfterExecuteAsync(RentalProcessInstance process, CreateRentalResult result, CancellationToken ct)
    {
        // No post-execution steps 
    }

    /// <inheritdoc />
    protected override async Task<BlankActionResult> BeforeExecuteAsync(
        RentalProcessInstance process, CreateRentalInput input, CancellationToken ct)
    {
        if (input.RequestedEnd <= input.RequestedStart)
            return new BlankActionResult
            {
                Success = false,
                ActionName = nameof(RentalActionType.CreateRental),
                Errors = new() { ["RequestedEnd"] = "Requested end must be after the requested start." }
            };

        return new BlankActionResult
        {
            Success = true,
            ActionName = nameof(RentalActionType.CreateRental)
        };
    }

    /// <inheritdoc />
    protected override async Task<CreateRentalResult> ExecuteAsync(
        RentalProcessInstance process, CreateRentalInput input, CancellationToken ct)
    {
        var now = SystemClock.Instance.GetCurrentInstant();

        var rental = new Rental
        {
            Uuid = Guid.NewGuid(),
            CustomerKcId = input.ActorKcId,
            CustomerName = input.CustomerName,
            CustomerEmail = input.CustomerEmail,
            Purpose = input.Purpose,
            RequestedStart = input.RequestedStart,
            RequestedEnd = input.RequestedEnd,
            Notes = input.Notes,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddRentalAsync(rental, ct);

        process.Rental = rental;

        return new CreateRentalResult
        {
            Success = true,
            ActionName = nameof(RentalActionType.CreateRental),
            NewStage = RentalStage.Requested,
            ProcessInstanceGuid = process.Guid
        };
    }
}
