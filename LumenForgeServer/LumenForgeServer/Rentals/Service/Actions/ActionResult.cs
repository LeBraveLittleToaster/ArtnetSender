using LumenForgeServer.Rentals.Domain;
using NodaTime;

namespace LumenForgeServer.Rentals.Service.Actions;

/// <summary>
/// Abstract base for all rental action outcomes. Returned by every
/// <see cref="IRentalActionHandler"/> lifecycle method and forwarded to the caller
/// by the <see cref="RentalActionService"/> orchestrator.
/// </summary>
/// <remarks>
/// <para>
/// Handlers must subclass this to indicate their specific return contract:
/// - <see cref="BlankActionResult"/>: Action completes without returning domain data
/// - Specific subclass: Action returns domain objects (e.g., newly created GUIDs)
/// </para>
/// <para>
/// This enforces type safety—handlers cannot accidentally return the wrong result type,
/// and the API contract is explicit about what data each endpoint provides.
/// </para>
/// </remarks>
public abstract class ActionResult
{
    /// <summary>Whether the action completed without errors.</summary>
    public bool Success { get; init; }

    /// <summary>Canonical name of the action (e.g. <c>"Rental.Create"</c>).</summary>
    public string ActionName { get; init; } = "";

    /// <summary>UTC instant the result was produced.</summary>
    public Instant Timestamp { get; init; } = SystemClock.Instance.GetCurrentInstant();

    /// <summary>
    /// Stage the process should transition to after a successful execution.
    /// <c>null</c> means the stage remains unchanged.
    /// </summary>
    public RentalStage? NewStage { get; init; }

    /// <summary>
    /// Keyed error messages. Empty on success.
    /// Keys are field names or logical identifiers; values are human-readable messages.
    /// </summary>
    public Dictionary<string, string> Errors { get; init; } = new();
}

/// <summary>
/// Result for actions that don't return additional domain data—only success/failure and stage transitions.
/// Suitable for simple state-machine transitions like ApproveRequest, RecordPickup, etc.
/// </summary>
public sealed class BlankActionResult : ActionResult
{
    /// <summary>
    /// Executes the blank action result operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <returns>The operation result.</returns>
    public BlankActionResult() { }
    /// <summary>Creates a successful result that optionally transitions the process to a new stage.</summary>
    /// <param name="actionName">Text input used by this operation.</param>
    /// <param name="newStage">Input value used by this operation.</param>
    public static BlankActionResult Ok(string actionName, RentalStage? newStage = null) =>
        new() { Success = true, ActionName = actionName, NewStage = newStage };

    /// <summary>Creates a failed result carrying the provided error details.</summary>
    /// <param name="actionName">Text input used by this operation.</param>
    /// <param name="errors">Text input used by this operation.</param>
    public static BlankActionResult Fail(string actionName, Dictionary<string, string> errors) =>
        new() { Success = false, ActionName = actionName, Errors = errors };

    /// <summary>Creates a failed result with a single error entry.</summary>
    /// <param name="actionName">Text input used by this operation.</param>
    /// <param name="key">Text input used by this operation.</param>
    /// <param name="message">Text input used by this operation.</param>
    public static BlankActionResult Fail(string actionName, string key, string message) =>
        Fail(actionName, new Dictionary<string, string> { [key] = message });
}
