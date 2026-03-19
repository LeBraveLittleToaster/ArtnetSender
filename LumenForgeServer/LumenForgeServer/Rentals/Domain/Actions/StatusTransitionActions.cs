namespace LumenForgeServer.Rentals.Domain.Actions;

/// <summary>Records approval of a rental request. Status → Approved.</summary>
public sealed class ApproveRequestAction : RentalAction { }

/// <summary>Records that pickup was processed. Status → PickedUp.</summary>
public sealed class RecordPickupAction : RentalAction { }

/// <summary>Records that return/dropoff was processed. Status → Returned.</summary>
public sealed class RecordReturnAction : RentalAction { }

/// <summary>Records that the rental was completed. Status → Completed.</summary>
public sealed class CompleteRentalAction : RentalAction { }
