using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Persistence;
using NodaTime;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="ApproveExtensionHandler"/>.</summary>
public sealed class ApproveExtensionInput : ActionInput
{
    /// <summary>GUID of the extension request being approved.</summary>
    public required Guid ExtensionGuid { get; init; }

    /// <summary>Optional comment from the approver.</summary>
    public string? Comment { get; init; }
}

/// <summary>
/// Approves a previously submitted extension request, updating the
/// rental period end date accordingly.
/// </summary>
public sealed class ApproveExtensionHandler(IRentalProcessRepository repository)
    : RentalActionHandlerBase<ApproveExtensionInput, BlankActionResult>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.ApproveExtension;

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
    protected override Task AfterExecuteAsync(RentalProcessInstance process, BlankActionResult result, CancellationToken ct)
    {
        return Task.CompletedTask;
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
    protected override async Task<BlankActionResult> BeforeExecuteAsync(RentalProcessInstance process, ApproveExtensionInput input, CancellationToken ct)
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
        RentalProcessInstance process, ApproveExtensionInput input, CancellationToken ct)
    {
        var extension = await repository.GetExtensionByGuidAsync(input.ExtensionGuid, ct)
            ?? throw new NotFoundException($"Extension '{input.ExtensionGuid}' not found.");

        if (extension.IsApproved.HasValue)
            return BlankActionResult.Fail(nameof(RentalActionType.ApproveExtension), "Extension",
                "Extension has already been reviewed.");

        var now = SystemClock.Instance.GetCurrentInstant();

        extension.IsApproved = true;
        extension.ReviewComment = input.Comment;
        extension.ReviewedByKcId = input.ActorKcId;
        extension.ReviewedAt = now;

        if (process.Rental is not null)
        {
            process.Rental.RequestedEnd = extension.NewRequestedEnd;
            process.Rental.UpdatedAt = now;
        }

        return BlankActionResult.Ok(nameof(RentalActionType.ApproveExtension));
    }
}
