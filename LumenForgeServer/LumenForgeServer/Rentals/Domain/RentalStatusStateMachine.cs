namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Defines and validates allowed rental status transitions.
/// </summary>
public static class RentalStatusStateMachine
{
    private static readonly IReadOnlyDictionary<RentalStatus, IReadOnlySet<RentalStatus>> AllowedTransitions =
        new Dictionary<RentalStatus, IReadOnlySet<RentalStatus>>
        {
            [RentalStatus.Requested] = new HashSet<RentalStatus>([RentalStatus.Approved, RentalStatus.Rejected, RentalStatus.Cancelled]),
            [RentalStatus.Approved] = new HashSet<RentalStatus>([RentalStatus.PickedUp, RentalStatus.Cancelled]),
            [RentalStatus.PickedUp] = new HashSet<RentalStatus>([RentalStatus.Returned, RentalStatus.Scrapped]),
            [RentalStatus.Returned] = new HashSet<RentalStatus>([RentalStatus.Completed, RentalStatus.Scrapped]),
            [RentalStatus.Rejected] = new HashSet<RentalStatus>(),
            [RentalStatus.Cancelled] = new HashSet<RentalStatus>(),
            [RentalStatus.Completed] = new HashSet<RentalStatus>(),
            [RentalStatus.Scrapped] = new HashSet<RentalStatus>(),
        };

    public static bool CanTransition(RentalStatus fromStatus, RentalStatus toStatus)
    {
        if (fromStatus == toStatus)
        {
            return true;
        }

        return AllowedTransitions.TryGetValue(fromStatus, out var allowed)
               && allowed.Contains(toStatus);
    }

    public static IReadOnlySet<RentalStatus> GetAllowedTargets(RentalStatus fromStatus)
    {
        return AllowedTransitions.TryGetValue(fromStatus, out var allowed)
            ? allowed
            : new HashSet<RentalStatus>();
    }
}
