using LumenForgeServer.Billing.Domain;
using LumenForgeServer.Common;
using LumenForgeServer.Common.Database;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="RecordPaymentHandler"/>.</summary>
public sealed class RecordPaymentInput : ActionInput
{
    /// <summary>GUID of the invoice being paid.</summary>
    public required Guid InvoiceGuid { get; init; }

    /// <summary>Amount paid by the customer.</summary>
    public required decimal Amount { get; init; }

    /// <summary>Payment method used.</summary>
    public required PaymentMethod Method { get; init; }

    /// <summary>External payment reference (e.g. transaction id).</summary>
    public string? Reference { get; init; }
}

/// <summary>
/// Records a payment against the rental invoice.
/// Transitions the process to <see cref="RentalStage.Paid"/>.
/// </summary>
public sealed class RecordPaymentHandler(AppDbContext db)
    : RentalActionHandlerBase<RecordPaymentInput, BlankActionResult>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.RecordPayment;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.Invoiced };

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

    /// <inheritdoc />
    /// <summary>
    /// Executes the before execute async operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="process">Input value used by this operation.</param>
    /// <param name="input">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the BlankActionResult result.</returns>
    protected override async Task<BlankActionResult> BeforeExecuteAsync(
        RentalProcessInstance process, RecordPaymentInput input, CancellationToken ct)
    {
        if (input.Amount <= 0)
            return BlankActionResult.Fail(nameof(RentalActionType.RecordPayment), "Amount",
                "Payment amount must be greater than zero.");

        return BlankActionResult.Ok(nameof(RentalActionType.RecordPayment));
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
        RentalProcessInstance process, RecordPaymentInput input, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .FirstOrDefaultAsync(i => i.Uuid == input.InvoiceGuid, ct)
            ?? throw new NotFoundException($"Invoice '{input.InvoiceGuid}' not found.");

        var now = SystemClock.Instance.GetCurrentInstant();

        var paidStatus = await db.PaymentStatuses
            .FirstOrDefaultAsync(s => s.Name == "COMPLETED", ct);

        var payment = new Payment
        {
            Uuid = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            PaymentStatusId = paidStatus?.Id ?? 1,
            Amount = input.Amount,
            CurrencyCode = invoice.CurrencyCode,
            PaymentMethod = input.Method,
            ProviderReference = input.Reference,
            PaidAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        await db.Payments.AddAsync(payment, ct);

        invoice.PaidAt = now;
        invoice.UpdatedAt = now;

        return BlankActionResult.Ok(nameof(RentalActionType.RecordPayment), RentalStage.Paid);
    }
}
