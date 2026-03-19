namespace LumenForgeServer.Rentals.Domain.Actions;

/// <summary>Records that items were assigned to the rental.</summary>
public sealed class AssignItemsAction : RentalAction { }

/// <summary>Records that items were removed from the rental.</summary>
public sealed class RemoveItemsAction : RentalAction { }

/// <summary>Records that individual line items were approved.</summary>
public sealed class ApproveItemsAction : RentalAction { }

/// <summary>Records that individual line items were rejected.</summary>
public sealed class RejectItemsAction : RentalAction { }
