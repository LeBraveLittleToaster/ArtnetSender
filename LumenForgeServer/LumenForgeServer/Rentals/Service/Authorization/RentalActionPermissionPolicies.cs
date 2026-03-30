using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Rentals.Service.Actions;

namespace LumenForgeServer.Rentals.Service.Authorization;

/// <summary>
/// Immutable policy definition for a rental action authorization rule.
/// </summary>
/// <param name="Permission">Explicit permission required to execute the mapped action.</param>
/// <param name="ForbidIfActorOwnsRental">
/// When <see langword="true"/>, the action is denied if the actor is also the rental owner (self-approval guard).
/// </param>
internal sealed record RentalActionPermissionPolicy(
    Permissions Permission,
    bool ForbidIfActorOwnsRental = false);

/// <summary>
/// Central mapping between <see cref="RentalActionType"/> values and their permission/ownership policy.
/// Core concept: this dictionary defines action-level authorization semantics in one place so discovery and
/// execution use the same rule set.
/// </summary>
internal static class RentalActionPermissionPolicies
{
    private static readonly IReadOnlyDictionary<RentalActionType, RentalActionPermissionPolicy> Policies =
        new Dictionary<RentalActionType, RentalActionPermissionPolicy>
        {
            [RentalActionType.CreateRental] = new(Permissions.RentalActionCreateRental),
            [RentalActionType.ApproveRequest] = new(Permissions.RentalActionApproveRequest, ForbidIfActorOwnsRental: true),
            [RentalActionType.RejectRequest] = new(Permissions.RentalActionRejectRequest, ForbidIfActorOwnsRental: true),
            [RentalActionType.AssignItems] = new(Permissions.RentalActionAssignItems),
            [RentalActionType.RemoveItems] = new(Permissions.RentalActionRemoveItems),
            [RentalActionType.ApproveItems] = new(Permissions.RentalActionApproveItems, ForbidIfActorOwnsRental: true),
            [RentalActionType.RejectItems] = new(Permissions.RentalActionRejectItems, ForbidIfActorOwnsRental: true),
            [RentalActionType.GenerateChecklist] = new(Permissions.RentalActionGenerateChecklist),
            [RentalActionType.ScanChecklist] = new(Permissions.RentalActionScanChecklist),
            [RentalActionType.SignChecklist] = new(Permissions.RentalActionSignChecklist),
            [RentalActionType.RecordPickup] = new(Permissions.RentalActionRecordPickup),
            [RentalActionType.RecordReturn] = new(Permissions.RentalActionRecordReturn),
            [RentalActionType.RequestExtension] = new(Permissions.RentalActionRequestExtension),
            [RentalActionType.ApproveExtension] = new(Permissions.RentalActionApproveExtension, ForbidIfActorOwnsRental: true),
            [RentalActionType.RejectExtension] = new(Permissions.RentalActionRejectExtension, ForbidIfActorOwnsRental: true),
            [RentalActionType.RecordDamages] = new(Permissions.RentalActionRecordDamages),
            [RentalActionType.CreateMaintenanceJobs] = new(Permissions.RentalActionCreateMaintenanceJobs),
            [RentalActionType.GenerateInvoice] = new(Permissions.RentalActionGenerateInvoice),
            [RentalActionType.RecordPayment] = new(Permissions.RentalActionRecordPayment),
            [RentalActionType.GenerateReport] = new(Permissions.RentalActionGenerateReport),
            [RentalActionType.CompleteRental] = new(Permissions.RentalActionCompleteRental),
            [RentalActionType.CancelRental] = new(Permissions.RentalActionCancelRental),
            [RentalActionType.ScrapRental] = new(Permissions.RentalActionScrapRental)
        };

    /// <summary>
    /// Resolves the configured authorization policy for a rental action type.
    /// </summary>
    /// <remarks>Potential side effects: read-only lookup in in-memory configuration.</remarks>
    /// <param name="actionType">Rental action type to resolve.</param>
    /// <returns>The mapped permission policy that must be enforced for the action.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when no policy exists for the given action type.</exception>
    public static RentalActionPermissionPolicy Get(RentalActionType actionType)
    {
        return Policies.TryGetValue(actionType, out var policy)
            ? policy
            : throw new ArgumentOutOfRangeException(nameof(actionType), actionType, "No action policy configured.");
    }
}
