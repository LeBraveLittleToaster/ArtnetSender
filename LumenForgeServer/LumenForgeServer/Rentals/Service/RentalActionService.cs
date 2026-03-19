using System.Text.Json;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Rentals.Actions;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Domain.Actions;
using LumenForgeServer.Rentals.Dto.Query;
using LumenForgeServer.Rentals.Dto.View;
using LumenForgeServer.Rentals.Persistence;

namespace LumenForgeServer.Rentals.Service;

/// <summary>
/// Orchestrates the execution of rental actions, resolving handlers via DI
/// and persisting the resulting <see cref="RentalAction"/> audit records.
/// </summary>
public sealed class RentalActionService
{
    private readonly IRentalRepository _repository;
    private readonly Dictionary<RentalActionType, IRentalActionHandler> _handlers;

    public RentalActionService(IRentalRepository repository, IEnumerable<IRentalActionHandler> handlers)
    {
        _repository = repository;
        _handlers = handlers.ToDictionary(h => h.ActionType);
    }

    /// <summary>
    /// Returns every action type that can currently be executed on the given rental.
    /// </summary>
    public async Task<IReadOnlyList<AvailableActionView>> GetAvailableActionsAsync(
        Guid rentalGuid, CancellationToken ct)
    {
        var rental = await _repository.GetRentalByGuidAsync(
                rentalGuid,
                RentalInclude.Items | RentalInclude.Checklists | RentalInclude.Extensions,
                ct)
            ?? throw new NotFoundException($"Rental '{rentalGuid}' not found.");

        return _handlers.Values
            .Where(h => h.CanExecute(rental))
            .Select(h => new AvailableActionView { ActionType = h.ActionType })
            .OrderBy(a => a.ActionType)
            .ToList();
    }

    /// <summary>
    /// Executes a single action on the rental, persists the audit record, and returns
    /// the action view.
    /// </summary>
    public async Task<RentalActionView> ExecuteActionAsync(
        Guid rentalGuid,
        RentalActionType actionType,
        JsonElement? input,
        string actorUserId,
        CancellationToken ct)
    {
        if (!_handlers.TryGetValue(actionType, out var handler))
        {
            throw new ValidationException(
                $"Unknown action type '{actionType}'.",
                new Dictionary<string, string[]>
                {
                    ["action_type"] = [$"'{actionType}' is not a recognised action."]
                });
        }

        var rental = await _repository.GetRentalByGuidAsync(
                rentalGuid,
                RentalInclude.Items | RentalInclude.Checklists | RentalInclude.Extensions,
                ct)
            ?? throw new NotFoundException($"Rental '{rentalGuid}' not found.");

        if (!handler.CanExecute(rental))
        {
            throw new ValidationException(
                $"Action '{actionType}' cannot be executed on rental '{rentalGuid}' in status '{rental.RentalStatus}'.",
                new Dictionary<string, string[]>
                {
                    ["action_type"] = [$"'{actionType}' is not available for the current rental state."]
                });
        }

        var action = await handler.ExecuteAsync(rental, input, actorUserId, ct);

        rental.Actions.Add(action);
        await _repository.SaveChangesAsync(ct);

        return RentalActionView.FromEntity(action);
    }

    /// <summary>
    /// Lists the executed action history for a rental.
    /// </summary>
    public async Task<(IReadOnlyList<RentalActionView> items, long total)> ListActionsAsync(
        Guid rentalGuid, int limit, int offset, CancellationToken ct)
    {
        var exists = await _repository.RentalExistsByGuidAsync(rentalGuid, ct);
        if (!exists)
        {
            throw new NotFoundException($"Rental '{rentalGuid}' not found.");
        }

        var (actions, total) = await _repository.ListActionsForRentalAsync(rentalGuid, limit, offset, ct);
        return (actions.Select(RentalActionView.FromEntity).ToList(), total);
    }
}
