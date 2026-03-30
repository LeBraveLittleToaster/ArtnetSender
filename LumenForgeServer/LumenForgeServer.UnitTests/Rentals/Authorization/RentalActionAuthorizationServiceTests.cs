using System.Security.Claims;
using FluentAssertions;
using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Persistence;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Persistence;
using LumenForgeServer.Rentals.Service;
using LumenForgeServer.Rentals.Service.Actions;
using LumenForgeServer.Rentals.Service.Authorization;
using LumenForgeServer.Rentals.Service.Authorization.Dto;
using LumenForgeServer.UnitTests.Rentals.Actions.Helpers;
using NSubstitute;

namespace LumenForgeServer.UnitTests.Rentals.Authorization;

public class RentalActionAuthorizationServiceTests
{
    private readonly IRentalProcessRepository _processRepository = Substitute.For<IRentalProcessRepository>();
    private readonly IAuthRepository _authRepository = Substitute.For<IAuthRepository>();
    private readonly IRentalActionRegistry _actionRegistry = new RentalActionRegistry();
    private readonly CancellationToken _ct = CancellationToken.None;

    private RentalActionAuthorizationService CreateService()
    {
        var accessService = new RentalAccessService(_authRepository);
        return new RentalActionAuthorizationService(_processRepository, _actionRegistry, accessService);
    }

    [Fact]
    public async Task GetAvailableActions_OwnerCannotApproveOwnProcess()
    {
        var process = HandlerTestHelper.CreateProcess(RentalStage.Requested);
        process.Rental!.CustomerKcId = "owner-kc-id";
        _processRepository.GetByGuidAsync(process.Guid, _ct).Returns(process);

        var user = CreateUser(
            "owner-kc-id",
            Permissions.RentalUserOwn,
            Permissions.RentalActionApproveRequest,
            Permissions.RentalActionRejectRequest,
            Permissions.RentalActionCancelRental);

        var service = CreateService();
        var result = await service.GetAvailableActionsAsync(
            new GetAvailableRentalActionsRequestDto
            {
                User = user,
                ProcessGuid = process.Guid
            },
            _ct);

        result.Status.Should().Be(RentalActionAuthorizationStatus.Allowed);
        result.Actions.Should().Contain(RentalActionType.CancelRental);
        result.Actions.Should().NotContain(RentalActionType.ApproveRequest);
        result.Actions.Should().NotContain(RentalActionType.RejectRequest);
    }

    [Fact]
    public async Task GetAvailableActions_ReadScopeOnly_ReturnsEmptyActionList()
    {
        var process = HandlerTestHelper.CreateProcess(RentalStage.Requested);
        _processRepository.GetByGuidAsync(process.Guid, _ct).Returns(process);

        var user = CreateUser(
            "reader",
            Permissions.RentalReadAll,
            Permissions.RentalActionApproveRequest,
            Permissions.RentalActionRejectRequest,
            Permissions.RentalActionCancelRental);

        var service = CreateService();
        var result = await service.GetAvailableActionsAsync(
            new GetAvailableRentalActionsRequestDto
            {
                User = user,
                ProcessGuid = process.Guid
            },
            _ct);

        result.Status.Should().Be(RentalActionAuthorizationStatus.Allowed);
        result.Actions.Should().BeEmpty();
    }

    [Fact]
    public async Task AuthorizeAction_OutOfScope_ReturnsForbidden()
    {
        var process = HandlerTestHelper.CreateProcess(RentalStage.Requested);
        var processGroupGuid = Guid.NewGuid();
        process.Rental!.GroupGuid = processGroupGuid;
        _processRepository.GetByGuidAsync(process.Guid, _ct).Returns(process);

        _authRepository.GetGroupGuidsForUserAsync("group-user", _ct)
            .Returns(new HashSet<Guid> { Guid.NewGuid() });

        var user = CreateUser(
            "group-user",
            Permissions.RentalGroup,
            Permissions.RentalActionApproveRequest);

        var service = CreateService();
        var result = await service.AuthorizeActionAsync(
            new AuthorizeRentalActionRequestDto
            {
                User = user,
                ProcessGuid = process.Guid,
                ActionType = RentalActionType.ApproveRequest
            },
            _ct);

        result.Status.Should().Be(RentalActionAuthorizationStatus.Forbidden);
        result.Reason.Should().Be(RentalActionAuthorizationReason.OutOfScope);
    }

    [Fact]
    public async Task AuthorizeAction_StageNotAllowed_ReturnsForbidden()
    {
        var process = HandlerTestHelper.CreateProcess(RentalStage.Approved);
        _processRepository.GetByGuidAsync(process.Guid, _ct).Returns(process);

        var user = CreateUser(
            "admin",
            Permissions.RentalUpdateAll,
            Permissions.RentalActionApproveRequest);

        var service = CreateService();
        var result = await service.AuthorizeActionAsync(
            new AuthorizeRentalActionRequestDto
            {
                User = user,
                ProcessGuid = process.Guid,
                ActionType = RentalActionType.ApproveRequest
            },
            _ct);

        result.Status.Should().Be(RentalActionAuthorizationStatus.Forbidden);
        result.Reason.Should().Be(RentalActionAuthorizationReason.StageNotAllowed);
    }

    [Fact]
    public async Task AuthorizeCreateAction_RequiresPermissionAndScope()
    {
        var userMissingPermission = CreateUser("owner-kc-id", Permissions.RentalUserOwn);
        var service = CreateService();

        var forbidden = await service.AuthorizeCreateActionAsync(
            new AuthorizeCreateRentalActionRequestDto
            {
                User = userMissingPermission
            },
            _ct);

        forbidden.Status.Should().Be(RentalActionAuthorizationStatus.Forbidden);
        forbidden.Reason.Should().Be(RentalActionAuthorizationReason.MissingActionPermission);

        var allowedUser = CreateUser(
            "owner-kc-id",
            Permissions.RentalUserOwn,
            Permissions.RentalActionCreateRental);

        var allowed = await service.AuthorizeCreateActionAsync(
            new AuthorizeCreateRentalActionRequestDto
            {
                User = allowedUser
            },
            _ct);

        allowed.Status.Should().Be(RentalActionAuthorizationStatus.Allowed);
        allowed.Reason.Should().Be(RentalActionAuthorizationReason.None);
    }

    [Fact]
    public async Task UpdateAllPermission_AllowsApproveOwnRequestWithoutActionPermission()
    {
        var process = HandlerTestHelper.CreateProcess(RentalStage.Requested);
        process.Rental!.CustomerKcId = "owner-kc-id";
        _processRepository.GetByGuidAsync(process.Guid, _ct).Returns(process);

        var user = CreateUser(
            "owner-kc-id",
            Permissions.RentalReadAll,
            Permissions.RentalUpdateAll);

        var service = CreateService();
        var available = await service.GetAvailableActionsAsync(
            new GetAvailableRentalActionsRequestDto
            {
                User = user,
                ProcessGuid = process.Guid
            },
            _ct);

        available.Status.Should().Be(RentalActionAuthorizationStatus.Allowed);
        available.Actions.Should().Contain(RentalActionType.ApproveRequest);

        var authorize = await service.AuthorizeActionAsync(
            new AuthorizeRentalActionRequestDto
            {
                User = user,
                ProcessGuid = process.Guid,
                ActionType = RentalActionType.ApproveRequest
            },
            _ct);

        authorize.Status.Should().Be(RentalActionAuthorizationStatus.Allowed);
    }

    private static ClaimsPrincipal CreateUser(string sub, params Permissions[] permissions)
    {
        var claims = new List<Claim>
        {
            new("sub", sub),
            new(ClaimTypes.NameIdentifier, sub)
        };
        claims.AddRange(permissions.Select(p => new Claim(ClaimTypes.Role, p.ToString())));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }
}
