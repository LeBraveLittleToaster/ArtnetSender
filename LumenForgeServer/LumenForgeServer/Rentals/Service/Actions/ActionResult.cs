using LumenForgeServer.Rentals.Domain;
using NodaTime;

namespace LumenForgeServer.Rentals.Service.Actions;

/// <summary>
/// Outcome of a single action execution. Returned by every
/// <see cref="IRentalActionHandler"/> lifecycle method and forwarded to the caller
/// by the <see cref="RentalActionService"/> orchestrator.
/// </summary>
/// <remarks>
/// Handlers may subclass this to attach action-specific data (e.g., a newly
/// created <see cref="RentalProcessInstance"/> GUID). The orchestrator always
/// works with the base type so logging and stage transitions stay generic.
/// </remarks>
public class ActionResult
{
    /// <summary>Whether the action completed without errors.</summary>
    public bool Success { get; init; }

    /// <summary>Canonical name of the action (e.g. <c>"Rental.Create"</c>).</summary>
    public required string ActionName { get; init; }

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

    /// <summary>Creates a successful result that optionally transitions the process to a new stage.</summary>
    public static ActionResult Ok(string actionName, RentalStage? newStage = null) =>
        new() { Success = true, ActionName = actionName, NewStage = newStage };

    /// <summary>Creates a failed result carrying the provided error details.</summary>
    public static ActionResult Fail(string actionName, Dictionary<string, string> errors) =>
        new() { Success = false, ActionName = actionName, Errors = errors };

    /// <summary>Creates a failed result with a single error entry.</summary>
    public static ActionResult Fail(string actionName, string key, string message) =>
        Fail(actionName, new Dictionary<string, string> { [key] = message });
}
