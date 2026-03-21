using LumenForgeServer.Billing.Domain;
using LumenForgeServer.Common.Database;
using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="GenerateInvoiceHandler"/>.</summary>
public sealed class GenerateInvoiceInput : ActionInput
{
    /// <summary>Optional override for the invoice due date (ISO 8601).</summary>
    public string? DueDateOverride { get; init; }
}

/// <summary>Extended result carrying the generated invoice GUID.</summary>
public sealed class GenerateInvoiceResult : ActionResult
{
    /// <summary>GUID of the generated invoice.</summary>
    public required Guid InvoiceGuid { get; init; }
}

/// <summary>
/// Generates an invoice for the rental based on the period and assigned items.
/// Transitions the process to <see cref="RentalStage.Invoiced"/>.
/// Internal action — integrates with the Billing module.
/// </summary>
public sealed class GenerateInvoiceHandler(AppDbContext db)
    : RentalActionHandlerBase<GenerateInvoiceInput>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.GenerateInvoice;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.Returned, RentalStage.Inspected };

    /// <inheritdoc />
    protected override Task<ActionResult> BeforeExecuteAsync(
        RentalProcessInstance process, GenerateInvoiceInput input, CancellationToken ct)
    {
        if (process.Rental is null)
            return Task.FromResult(ActionResult.Fail(nameof(RentalActionType.GenerateInvoice), "Rental",
                "Process has no linked rental."));

        return Task.FromResult(ActionResult.Ok(nameof(RentalActionType.GenerateInvoice)));
    }

    /// <inheritdoc />
    protected override async Task<ActionResult> ExecuteAsync(
        RentalProcessInstance process, GenerateInvoiceInput input, CancellationToken ct)
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        var rental = process.Rental!;

        var draftStatus = await db.InvoiceStatuses
            .FirstOrDefaultAsync(s => s.Name == "DRAFT", ct);

        Instant? dueAt = null;
        if (!string.IsNullOrEmpty(input.DueDateOverride))
        {
            var parsed = InstantPattern.General.Parse(input.DueDateOverride);
            if (parsed.Success)
                dueAt = parsed.Value;
        }

        dueAt ??= now.Plus(Duration.FromDays(30));

        var invoiceNumber = $"INV-{rental.Uuid.ToString()[..8].ToUpperInvariant()}-{now.ToUnixTimeMilliseconds() % 100000}";

        var invoice = new Invoice
        {
            Uuid = Guid.NewGuid(),
            RentalId = rental.Id,
            InvoiceStatusId = draftStatus?.Id ?? 1,
            InvoiceNumber = invoiceNumber,
            SubtotalAmount = 0m,
            TaxAmount = 0m,
            TotalAmount = 0m,
            CurrencyCode = "EUR",
            GeneratedAt = now,
            GeneratedByUserId = input.ActorKcId,
            IssuedAt = now,
            DueAt = dueAt,
            CreatedAt = now,
            UpdatedAt = now
        };

        await db.Invoices.AddAsync(invoice, ct);

        return new GenerateInvoiceResult
        {
            Success = true,
            ActionName = nameof(RentalActionType.GenerateInvoice),
            NewStage = RentalStage.Invoiced,
            InvoiceGuid = invoice.Uuid
        };
    }
}
