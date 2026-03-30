using LumenForgeServer.Rentals.Service.Authorization.Dto;

namespace LumenForgeServer.Rentals.Service.Authorization;

/// <summary>
/// Provides a single authorization contract for rental action discovery and execution.
/// </summary>
public interface IRentalActionAuthorizationService
{
    /// <summary>
    /// Calculates the action list that should be exposed to the caller for a process.
    /// </summary>
    /// <param name="request">Authorization context containing caller and process identifier.</param>
    /// <param name="ct">Cancellation token for asynchronous authorization dependencies.</param>
    /// <returns>A status/result DTO with either allowed actions or a forbid/not-found decision.</returns>
    Task<GetAvailableRentalActionsResultDto> GetAvailableActionsAsync(
        GetAvailableRentalActionsRequestDto request,
        CancellationToken ct);

    /// <summary>
    /// Authorizes execution of a specific action for the caller on a process.
    /// </summary>
    /// <param name="request">Authorization context containing caller, process identifier, and action type.</param>
    /// <param name="ct">Cancellation token for asynchronous authorization dependencies.</param>
    /// <returns>A status/reason DTO indicating whether execution is allowed.</returns>
    Task<AuthorizeRentalActionResultDto> AuthorizeActionAsync(
        AuthorizeRentalActionRequestDto request,
        CancellationToken ct);

    /// <summary>
    /// Authorizes rental creation for the caller before workflow state is created.
    /// </summary>
    /// <param name="request">Authorization context containing caller identity and optional owning group.</param>
    /// <param name="ct">Cancellation token for asynchronous authorization dependencies.</param>
    /// <returns>A status/reason DTO indicating whether create is allowed.</returns>
    Task<AuthorizeCreateRentalActionResultDto> AuthorizeCreateActionAsync(
        AuthorizeCreateRentalActionRequestDto request,
        CancellationToken ct);
}
