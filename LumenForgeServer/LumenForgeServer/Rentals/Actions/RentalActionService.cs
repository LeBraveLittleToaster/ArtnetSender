using System.Text.Json;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Persistence;
using NodaTime;

namespace LumenForgeServer.Rentals.Actions;

/// <summary>
/// Central orchestrator for the rental action framework.
/// Loads a <see cref="RentalProcessInstance"/> by its GUID, resolves the matching
/// <see cref="IRentalActionHandler"/>, validates stage constraints, runs the
/// before → execute → after lifecycle, persists the stage transition, and writes
/// a <see cref="RentalActionLog"/> entry.
/// </summary>
/// <remarks>
/// Registered as a scoped service. All handlers are injected via
/// <c>IEnumerable&lt;IRentalActionHandler&gt;</c> and selected by
/// <see cref="RentalActionType"/> at runtime.
/// </remarks>
public sealed class RentalActionService(
    IEnumerable<IRentalActionHandler> handlers,
    IRentalProcessRepository processRepository,
    IRentalActionRegistry registry,
    ILogger<RentalActionService> logger)
{
    private readonly Dictionary<RentalActionType, IRentalActionHandler> _handlerMap =
        handlers.ToDictionary(h => h.ActionType);

    // ── Public API ──────────────────────────────────────────────────

    /// <summary>
    /// Executes an action on an existing process instance identified by
    /// <paramref name="processGuid"/>.
    /// </summary>
    /// <exception cref="NotFoundException">Thrown when no process with the given GUID exists.</exception>
    /// <exception cref="ValidationException">Thrown when the action is not allowed in the current stage.</exception>
    public async Task<ActionResult> ExecuteActionAsync(
        Guid processGuid,
        RentalActionType actionType,
        ActionInput input,
        CancellationToken ct)
    {
        var process = await processRepository.GetByGuidAsync(processGuid, ct)
            ?? throw new NotFoundException($"Process instance '{processGuid}' not found.");

        return await RunLifecycleAsync(process, actionType, input, ct);
    }

    /// <summary>
    /// Executes the <see cref="RentalActionType.CreateRental"/> action, which
    /// bootstraps a new <see cref="RentalProcessInstance"/> (no existing GUID required).
    /// </summary>
    public async Task<ActionResult> CreateProcessAsync(
        ActionInput input,
        CancellationToken ct)
    {
        var process = new RentalProcessInstance
        {
            Guid = Guid.NewGuid(),
            CurrentStage = RentalStage.None,
            CreatedByKcId = input.ActorKcId,
            CreatedAt = SystemClock.Instance.GetCurrentInstant(),
            UpdatedAt = SystemClock.Instance.GetCurrentInstant()
        };

        await processRepository.AddAsync(process, ct);

        return await RunLifecycleAsync(process, RentalActionType.CreateRental, input, ct);
    }

    /// <summary>
    /// Returns the actions available for the process identified by
    /// <paramref name="processGuid"/> based on its current stage.
    /// </summary>
    /// <exception cref="NotFoundException">Thrown when no process with the given GUID exists.</exception>
    public async Task<IReadOnlySet<RentalActionType>> GetAvailableActionsAsync(
        Guid processGuid,
        CancellationToken ct)
    {
        var process = await processRepository.GetByGuidAsync(processGuid, ct)
            ?? throw new NotFoundException($"Process instance '{processGuid}' not found.");

        return registry.GetAvailableActions(process.CurrentStage);
    }

    // ── Private helpers ─────────────────────────────────────────────

    /// <summary>
    /// Runs the full handler lifecycle: validate stage, before, execute, after, log.
    /// </summary>
    private async Task<ActionResult> RunLifecycleAsync(
        RentalProcessInstance process,
        RentalActionType actionType,
        ActionInput input,
        CancellationToken ct)
    {
        if (!_handlerMap.TryGetValue(actionType, out var handler))
        {
            throw new ValidationException(
                $"No handler registered for action '{actionType}'.",
                new Dictionary<string, string[]>
                {
                    ["actionType"] = [$"No handler registered for '{actionType}'."]
                });
        }

        var allowed = registry.GetAvailableActions(process.CurrentStage);
        if (!allowed.Contains(actionType))
        {
            throw new ValidationException(
                $"Action '{actionType}' is not allowed in stage '{process.CurrentStage}'.",
                new Dictionary<string, string[]>
                {
                    ["actionType"] = [$"Action '{actionType}' is not allowed in stage '{process.CurrentStage}'."]
                });
        }

        var stageBefore = process.CurrentStage;

        // ── Before ──
        var beforeResult = await handler.BeforeExecuteAsync(process, input, ct);
        if (!beforeResult.Success)
        {
            await WriteLogAsync(process, actionType, input, stageBefore, process.CurrentStage, beforeResult, ct);
            return beforeResult;
        }

        // ── Execute ──
        var result = await handler.ExecuteAsync(process, input, ct);

        // Apply stage transition if the handler signalled one
        if (result.Success && result.NewStage.HasValue)
        {
            process.CurrentStage = result.NewStage.Value;
            process.UpdatedAt = SystemClock.Instance.GetCurrentInstant();
            await processRepository.UpdateAsync(process, ct);
        }

        // ── After ──
        await handler.AfterExecuteAsync(process, result, ct);

        // ── Log & persist ──
        await WriteLogAsync(process, actionType, input, stageBefore, process.CurrentStage, result, ct);
        await processRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Action {Action} on process {ProcessGuid}: {Outcome} ({StageBefore} → {StageAfter})",
            actionType, process.Guid, result.Success ? "OK" : "FAIL", stageBefore, process.CurrentStage);

        return result;
    }

    /// <summary>
    /// Creates and persists a <see cref="RentalActionLog"/> entry.
    /// </summary>
    private async Task WriteLogAsync(
        RentalProcessInstance process,
        RentalActionType actionType,
        ActionInput input,
        RentalStage stageBefore,
        RentalStage stageAfter,
        ActionResult result,
        CancellationToken ct)
    {
        var log = new RentalActionLog
        {
            Guid = Guid.NewGuid(),
            ProcessInstanceId = process.Id,
            ActionType = actionType,
            PerformedByKcId = input.ActorKcId,
            StageBefore = stageBefore,
            StageAfter = stageAfter,
            Success = result.Success,
            ErrorMessage = result.Success ? null : JsonSerializer.Serialize(result.Errors),
            PayloadJson = JsonSerializer.Serialize(input, input.GetType()),
            PerformedAt = SystemClock.Instance.GetCurrentInstant()
        };

        await processRepository.AddActionLogAsync(log, ct);
    }
}
