using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Persistence;
using NodaTime;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="RequestExtensionHandler"/>.</summary>
public sealed class RequestExtensionInput : ActionInput
{
    /// <summary>New requested end date for the rental period.</summary>
    public required Instant NewRequestedEnd { get; init; }

    /// <summary>Reason the extension is needed.</summary>
    public string? Reason { get; init; }
}

/// <summary>Extended result carrying the new extension GUID.</summary>
public sealed class RequestExtensionResult : ActionResult
{
    /// <summary>GUID of the created extension request.</summary>
    public Guid? ExtensionGuid { get; init; } = null;
}

/// <summary>
/// Submits a request to extend the active rental period.
/// Does not change the stage — the extension must be approved or rejected separately.
/// External action typically initiated by the customer.
/// </summary>
public sealed class RequestExtensionHandler(IRentalProcessRepository repository)
    : RentalActionHandlerBase<RequestExtensionInput, RequestExtensionResult>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.RequestExtension;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.PickedUp };

    protected override async Task AfterExecuteAsync(RentalProcessInstance process, RequestExtensionResult result, CancellationToken ct)
    {
        
    }

    /// <inheritdoc />
    protected override async Task<BlankActionResult> BeforeExecuteAsync(
        RentalProcessInstance process, RequestExtensionInput input, CancellationToken ct)
    {
        if (process.Rental is null)
            return new BlankActionResult()
            {
                Success = false,
                ActionName = nameof(RentalActionType.RequestExtension),
                Errors = new() { ["Rental"] = "Process has no linked rental." }
            };

        if (input.NewRequestedEnd <= process.Rental.RequestedEnd)
            return new BlankActionResult
            {
                Success = false,
                ActionName = nameof(RentalActionType.RequestExtension),
                Errors = new() { ["NewRequestedEnd"] = "New end date must be after the current end date." }
            };

        return new BlankActionResult
        {
            Success = true,
            ActionName = nameof(RentalActionType.RequestExtension)
        };
    }

    /// <inheritdoc />
    protected override async Task<RequestExtensionResult> ExecuteAsync(
        RentalProcessInstance process, RequestExtensionInput input, CancellationToken ct)
    {
        var extension = new RentalExtension
        {
            Guid = Guid.NewGuid(),
            ProcessInstanceId = process.Id,
            NewRequestedEnd = input.NewRequestedEnd,
            OriginalEnd = process.Rental!.RequestedEnd,
            Reason = input.Reason,
            RequestedByKcId = input.ActorKcId,
            RequestedAt = SystemClock.Instance.GetCurrentInstant()
        };

        await repository.AddExtensionAsync(extension, ct);

        return new RequestExtensionResult
        {
            Success = true,
            ActionName = nameof(RentalActionType.RequestExtension),
            ExtensionGuid = extension.Guid
        };
    }
}
