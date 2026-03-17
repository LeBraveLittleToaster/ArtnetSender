using LumenForgeServer.Common;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Dto.Command;
using LumenForgeServer.Rentals.Dto.Query;
using LumenForgeServer.Rentals.Dto.View;
using LumenForgeServer.Rentals.Persistence;
using NodaTime;

namespace LumenForgeServer.Rentals.Service;

/// <summary>
/// Application service for generating, inspecting, and signing rental checklists.
/// </summary>
/// <remarks>
/// Workflow:
/// <list type="number">
///   <item>Staff calls <c>GenerateChecklist(PICKUP)</c> — one <see cref="ChecklistItem"/> row is created
///         per approved <see cref="RentalItem"/>, all with <c>is_checked = false</c>.</item>
///   <item>Staff submits inspection results one item at a time via <c>UpdateChecklistItem</c>.
///         A checklist may be partially complete — only the submitted rows have <c>is_checked = true</c>.</item>
///   <item>Staff calls <c>SignChecklist</c> to finalise the checklist. The checklist becomes immutable.</item>
///   <item>At return time, staff calls <c>GenerateChecklist(DROPOFF, sourceChecklistGuid)</c> which mirrors
///         the items from the referenced PICKUP checklist into a new DROPOFF checklist.</item>
/// </list>
/// </remarks>
public class ChecklistService(IRentalRepository repository)
{
    /// <summary>
    /// Generates a new checklist for a rental.
    /// PICKUP checklists are seeded from the rental's approved line items.
    /// DROPOFF checklists are seeded by mirroring the items of the referenced PICKUP checklist.
    /// </summary>
    public async Task<ChecklistView> GenerateChecklist(
        Guid rentalGuid, GenerateChecklistDto dto, string generatedByUserId, CancellationToken ct)
    {
        var rental = await repository.GetRentalByGuidAsync(rentalGuid, RentalInclude.Items, ct)
            ?? throw new NotFoundException($"Rental '{rentalGuid}' not found.");

        var now = SystemClock.Instance.GetCurrentInstant();
        long? sourceChecklistId = null;
        List<ChecklistItem> items;

        if (dto.ChecklistType == ChecklistType.DROPOFF)
        {
            if (!dto.SourceChecklistGuid.HasValue)
            {
                throw new ValidationException(
                    "source_checklist_guid is required for DROPOFF checklists.",
                    new Dictionary<string, string[]>
                    {
                        ["source_checklist_guid"] = ["Required when checklist_type is DROPOFF."]
                    });
            }

            var source = await repository.GetChecklistByGuidAsync(rentalGuid, dto.SourceChecklistGuid.Value, ct)
                ?? throw new NotFoundException(
                    $"Source checklist '{dto.SourceChecklistGuid}' not found on rental '{rentalGuid}'.");

            if (source.ChecklistType != ChecklistType.PICKUP)
            {
                throw new ValidationException(
                    "Source checklist must be of type PICKUP.",
                    new Dictionary<string, string[]>
                    {
                        ["source_checklist_guid"] = ["Must reference a PICKUP checklist."]
                    });
            }

            sourceChecklistId = source.Id;
            items = source.Items
                .Select(sci => BuildUncheckedItem(sci.RentalItemId, now))
                .ToList();
        }
        else
        {
            var approvedItems = rental.Items
                .Where(i => i.Status is RentalItemStatus.APPROVED or RentalItemStatus.PARTIALLY_APPROVED)
                .ToList();

            if (approvedItems.Count == 0)
            {
                throw new ValidationException(
                    "No approved rental items found; cannot generate a PICKUP checklist.",
                    new Dictionary<string, string[]>
                    {
                        ["items"] = ["At least one item with status APPROVED or PARTIALLY_APPROVED is required."]
                    });
            }

            items = approvedItems
                .Select(ri => BuildUncheckedItem(ri.Id, now))
                .ToList();
        }

        var checklist = new Checklist
        {
            Uuid = Guid.NewGuid(),
            RentalId = rental.Id,
            ChecklistType = dto.ChecklistType,
            SourceChecklistId = sourceChecklistId,
            GeneratedAt = now,
            GeneratedByUserId = generatedByUserId,
            Notes = dto.Notes?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            Items = items,
        };

        await repository.AddChecklistAsync(checklist, ct);
        await repository.SaveChangesAsync(ct);

        var persisted = await repository.GetChecklistByGuidAsync(rentalGuid, checklist.Uuid, ct)
            ?? throw new NotFoundException("Checklist not found after creation.");

        return ChecklistView.FromEntity(persisted);
    }

    /// <summary>
    /// Returns a single checklist with all its items.
    /// </summary>
    public async Task<ChecklistView> GetChecklist(Guid rentalGuid, Guid checklistGuid, CancellationToken ct)
    {
        var checklist = await repository.GetChecklistByGuidAsync(rentalGuid, checklistGuid, ct)
            ?? throw new NotFoundException(
                $"Checklist '{checklistGuid}' not found on rental '{rentalGuid}'.");

        return ChecklistView.FromEntity(checklist);
    }

    /// <summary>
    /// Returns all checklists for a rental ordered by generation time.
    /// </summary>
    public async Task<(IReadOnlyList<ChecklistView> items, long total)> ListChecklists(Guid rentalGuid, int limit, int offset, CancellationToken ct)
    {
        if (!await repository.RentalExistsByGuidAsync(rentalGuid, ct))
            throw new NotFoundException($"Rental '{rentalGuid}' not found.");

        var (checklists, total) = await repository.ListChecklistsForRentalAsync(rentalGuid, limit, offset, ct);
        return (checklists.Select(ChecklistView.FromEntity).ToList(), total);
    }

    /// <summary>
    /// Submits an inspection result for a single checklist item, marking it as checked.
    /// Only allowed while the parent checklist is unsigned.
    /// </summary>
    public async Task<ChecklistItemView> UpdateChecklistItem(
        Guid rentalGuid, Guid checklistGuid, Guid itemGuid,
        UpdateChecklistItemDto dto, CancellationToken ct)
    {
        var item = await repository.GetChecklistItemByGuidAsync(rentalGuid, checklistGuid, itemGuid, ct)
            ?? throw new NotFoundException(
                $"Checklist item '{itemGuid}' not found on checklist '{checklistGuid}'.");

        if (item.Checklist.SignedAt.HasValue)
        {
            throw new ValidationException(
                "Cannot modify a signed checklist.",
                new Dictionary<string, string[]>
                {
                    ["checklist"] = ["Checklist has already been signed and is immutable."]
                });
        }

        item.IsChecked = true;
        item.QuantityChecked = dto.QuantityChecked;
        item.ConditionOk = dto.ConditionOk;
        item.ConditionNotes = dto.ConditionNotes?.Trim();
        item.DamagedQuantity = dto.DamagedQuantity;
        item.DamageSummary = dto.DamageSummary?.Trim();
        item.DamageDescription = dto.DamageDescription?.Trim();
        item.UpdatedAt = SystemClock.Instance.GetCurrentInstant();

        await repository.SaveChangesAsync(ct);

        return ChecklistItemView.FromEntity(item);
    }

    /// <summary>
    /// Signs and finalises a checklist. The checklist becomes immutable once signed.
    /// Signing is allowed regardless of partial completion — staff may sign with unchecked
    /// items if some devices could not be inspected.
    /// </summary>
    public async Task<ChecklistView> SignChecklist(
        Guid rentalGuid, Guid checklistGuid,
        SignChecklistDto dto, string signedByUserId, CancellationToken ct)
    {
        var checklist = await repository.GetChecklistByGuidAsync(rentalGuid, checklistGuid, ct)
            ?? throw new NotFoundException(
                $"Checklist '{checklistGuid}' not found on rental '{rentalGuid}'.");

        if (checklist.SignedAt.HasValue)
        {
            throw new ValidationException(
                "Checklist is already signed.",
                new Dictionary<string, string[]>
                {
                    ["checklist"] = ["Already signed — no further modifications are allowed."]
                });
        }

        var now = SystemClock.Instance.GetCurrentInstant();
        checklist.SignedAt = now;
        checklist.SignedByUserId = signedByUserId;
        if (dto.Notes is not null) checklist.Notes = dto.Notes.Trim();
        checklist.UpdatedAt = now;

        await repository.SaveChangesAsync(ct);

        var updated = await repository.GetChecklistByGuidAsync(rentalGuid, checklistGuid, ct)
            ?? throw new NotFoundException("Checklist not found after signing.");

        return ChecklistView.FromEntity(updated);
    }

    private static ChecklistItem BuildUncheckedItem(long rentalItemId, Instant now) => new()
    {
        Uuid = Guid.NewGuid(),
        RentalItemId = rentalItemId,
        IsChecked = false,
        QuantityChecked = 0,
        ConditionOk = true,
        DamagedQuantity = 0,
        CreatedAt = now,
        UpdatedAt = now,
    };

    /// <summary>
    /// Resolves the checklist item for a QR-scanned device so the mobile app can
    /// pre-populate the inspection form before submitting via <c>UpdateChecklistItem</c>.
    /// Returns 404 when the device has no corresponding item on this checklist.
    /// Rejects scans on signed checklists since no further updates are possible.
    /// </summary>
    public async Task<ChecklistItemView> ScanDeviceOnChecklist(
        Guid rentalGuid, Guid checklistGuid, Guid deviceGuid, CancellationToken ct)
    {
        var checklist = await repository.GetChecklistByGuidAsync(rentalGuid, checklistGuid, ct)
            ?? throw new NotFoundException(
                $"Checklist '{checklistGuid}' not found on rental '{rentalGuid}'.");

        if (checklist.SignedAt.HasValue)
        {
            throw new ValidationException(
                "Checklist is already signed; no further items can be checked.",
                new Dictionary<string, string[]>
                {
                    ["checklist"] = ["Checklist has been signed and is immutable."]
                });
        }

        var item = await repository.GetChecklistItemByDeviceGuidAsync(rentalGuid, checklistGuid, deviceGuid, ct)
            ?? throw new NotFoundException(
                $"Device '{deviceGuid}' has no corresponding item on checklist '{checklistGuid}'. " +
                "Ensure the device has a stock binding linked to an approved rental item.");

        return ChecklistItemView.FromEntity(item);
    }
}
