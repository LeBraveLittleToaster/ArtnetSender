using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Persistence;
using NodaTime;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="RejectExtensionHandler"/>.</summary>
public sealed class RejectExtensionInput : ActionInput
{
    /// <summary>GUID of the extension request being rejected.</summary>
    public required Guid ExtensionGuid { get; init; }

    /// <summary>Reason for rejecting the extension.</summary>
    public required string Reason { get; init; }
}

/// <summary>
/// Rejects a previously submitted extension request.
/// The rental period remains unchanged.
/// </summary>
public sealed class RejectExtensionHandler(IRentalProcessRepository repository)
    : RentalActionHandlerBase<RejectExtensionInput, BlankActionResult>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.RejectExtension;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.PickedUp };

    /// <summary>
    /// Executes the after execute async operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="process">Input value used by this operation.</param>
    /// <param name="result">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task AfterExecuteAsync(RentalProcessInstance process, BlankActionResult result, CancellationToken ct)
    {
        
    }

    /// <summary>
    /// Executes the before execute async operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="process">Input value used by this operation.</param>
    /// <param name="input">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the BlankActionResult result.</returns>
    protected override async Task<BlankActionResult> BeforeExecuteAsync(RentalProcessInstance process, RejectExtensionInput input, CancellationToken ct)
    {
        return BlankActionResult.Ok(this.ActionType.ToString());
    }

    /// <inheritdoc />
    /// <summary>
    /// Executes the execute async operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="process">Input value used by this operation.</param>
    /// <param name="input">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the BlankActionResult result.</returns>
    protected override async Task<BlankActionResult> ExecuteAsync(
        RentalProcessInstance process, RejectExtensionInput input, CancellationToken ct)
    {
        var extension = await repository.GetExtensionByGuidAsync(input.ExtensionGuid, ct)
            ?? throw new NotFoundException($"Extension '{input.ExtensionGuid}' not found.");

        if (extension.IsApproved.HasValue)
            return BlankActionResult.Fail(nameof(RentalActionType.RejectExtension), "Extension",
                "Extension has already been reviewed.");

        extension.IsApproved = false;
        extension.ReviewComment = input.Reason;
        extension.ReviewedByKcId = input.ActorKcId;
        extension.ReviewedAt = SystemClock.Instance.GetCurrentInstant();

        return BlankActionResult.Ok(nameof(RentalActionType.RejectExtension));
    }
}
