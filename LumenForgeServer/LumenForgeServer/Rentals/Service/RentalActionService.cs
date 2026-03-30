using System.Text.Json;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Persistence;
using LumenForgeServer.Rentals.Service.Actions;
using NodaTime;

namespace LumenForgeServer.Rentals.Service;

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
    /// <param name="processGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="actionType">Input value used by this operation.</param>
    /// <param name="input">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
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
    /// <param name="input">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
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

    // ── Private helpers ─────────────────────────────────────────────

    /// <summary>
    /// Runs the full handler lifecycle: validate stage, before, execute, after, log.
    /// </summary>
    /// <param name="process">Input value used by this operation.</param>
    /// <param name="actionType">Input value used by this operation.</param>
    /// <param name="input">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
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
    /// <param name="process">Input value used by this operation.</param>
    /// <param name="actionType">Input value used by this operation.</param>
    /// <param name="input">Request payload containing the input data required for the operation.</param>
    /// <param name="stageBefore">Input value used by this operation.</param>
    /// <param name="stageAfter">Input value used by this operation.</param>
    /// <param name="result">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
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
            ProcessInstance = process,
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
