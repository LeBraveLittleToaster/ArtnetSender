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
    : RentalActionHandlerBase<ApproveExtensionInput>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.ApproveExtension;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.PickedUp };

    /// <inheritdoc />
    protected override async Task<ActionResult> ExecuteAsync(
        RentalProcessInstance process, ApproveExtensionInput input, CancellationToken ct)
    {
        var extension = await repository.GetExtensionByGuidAsync(input.ExtensionGuid, ct)
            ?? throw new NotFoundException($"Extension '{input.ExtensionGuid}' not found.");

        if (extension.IsApproved.HasValue)
            return ActionResult.Fail(nameof(RentalActionType.ApproveExtension), "Extension",
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

        return ActionResult.Ok(nameof(RentalActionType.ApproveExtension));
    }
}
