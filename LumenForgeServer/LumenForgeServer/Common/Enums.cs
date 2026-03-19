namespace LumenForgeServer.Common;

/// <summary>
/// Checklist types associated with rental workflow stages.
/// </summary>
public enum ChecklistType
{
    /// <summary>Checklist completed at pickup.</summary>
    PICKUP,
    /// <summary>Checklist completed at dropoff.</summary>
    DROPOFF
}

/// <summary>
/// Supported payment methods for billing.
/// </summary>
public enum PaymentMethod
{
    /// <summary>Cash payment.</summary>
    CASH,
    /// <summary>Card payment.</summary>
    CARD,
    /// <summary>Bank transfer or equivalent.</summary>
    TRANSFER,
    /// <summary>Any other payment method.</summary>
    OTHER
}

/// <summary>
/// Lifecycle status of a single rental line item.
/// </summary>
public enum RentalItemStatus
{
    /// <summary>Customer has requested this device; awaiting staff approval.</summary>
    REQUESTED,
    /// <summary>Staff has approved the full requested quantity.</summary>
    APPROVED,
    /// <summary>Staff has approved only part of the requested quantity.</summary>
    PARTIALLY_APPROVED,
    /// <summary>Staff has rejected the item request.</summary>
    REJECTED,
    /// <summary>Device has been picked up by the customer.</summary>
    PICKED_UP,
    /// <summary>Only part of the picked-up quantity has been returned.</summary>
    PARTIALLY_RETURNED,
    /// <summary>The full approved quantity has been returned.</summary>
    RETURNED,
    /// <summary>The device was reported lost and not returned.</summary>
    LOST,
    /// <summary>The device was returned but recorded as damaged.</summary>
    DAMAGED
}

/// <summary>
/// Priority level for a rental request.
/// </summary>
public enum RentalPriority
{
    /// <summary>Non-urgent, flexible timeline.</summary>
    LOW,
    /// <summary>Standard priority.</summary>
    NORMAL,
    /// <summary>Elevated priority requiring prompt attention.</summary>
    HIGH,
    /// <summary>Critical priority requiring immediate action.</summary>
    URGENT
}

/// <summary>
/// Type of event recorded in the rental audit log.
/// </summary>
public enum RentalEventType
{
    /// <summary>Rental was created by the customer.</summary>
    CREATED,
    /// <summary>The overall rental status was changed.</summary>
    STATUS_CHANGED,
    /// <summary>A new line item was added to the rental.</summary>
    ITEM_ADDED,
    /// <summary>A line item was removed from the rental.</summary>
    ITEM_REMOVED,
    /// <summary>A line item was approved by staff.</summary>
    ITEM_APPROVED,
    /// <summary>A line item was rejected by staff.</summary>
    ITEM_REJECTED,
    /// <summary>The rental was assigned to a staff member.</summary>
    ASSIGNED,
    /// <summary>Pickup was completed and devices were handed over.</summary>
    PICKUP_COMPLETED,
    /// <summary>Dropoff was completed and devices were returned.</summary>
    DROPOFF_COMPLETED,
    /// <summary>A rental extension was requested or approved.</summary>
    EXTENDED,
    /// <summary>The rental was cancelled.</summary>
    CANCELLED,
    /// <summary>The rental was marked as completed.</summary>
    COMPLETED,
    /// <summary>An invoice was generated for the rental.</summary>
    INVOICED,
    /// <summary>Payment was received for the rental.</summary>
    PAID,
    /// <summary>The final rental report was generated.</summary>
    REPORT_GENERATED
}

/// <summary>
/// Severity classification for a device damage report.
/// </summary>
public enum DamageSeverity
{
    /// <summary>Cosmetic or insignificant damage with no functional impact.</summary>
    MINOR,
    /// <summary>Noticeable damage that affects appearance or minor function.</summary>
    MODERATE,
    /// <summary>Significant damage affecting core functionality.</summary>
    SEVERE,
    /// <summary>Device is beyond repair; total write-off.</summary>
    TOTAL_LOSS
}
