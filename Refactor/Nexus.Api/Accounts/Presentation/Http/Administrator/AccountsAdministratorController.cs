using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Commands;
using Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Queries;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.CreateAccount;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.DisableAccount;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.EnableAccount;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.GrantAccountPermission;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.GrantAccountRole;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.ResetAccountPassword;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.RevokeAccountPermission;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.RevokeAccountRole;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Queries.GetAccountById;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Queries.SearchAccounts;
using Refactor.Nexus.Api.Accounts.Presentation.Http.Administrator.Contracts;
using Refactor.Nexus.Api.Shared.Controllers;

namespace Refactor.Nexus.Api.Accounts.Presentation.Http.Administrator;

[Route("api/accounts/administrator")]
[Authorize]
public sealed class AccountsAdministratorController : ApiControllerBase
{
    private const string AdministratorCreateTokenHeader = "X-Administrator-Create-Token";

    private readonly ICreateAccountUseCase _createAccount;
    private readonly ISearchAccountsUseCase _searchAccounts;
    private readonly IGetAccountByIdUseCase _getAccountById;
    private readonly IGrantAccountRoleUseCase _grantAccountRole;
    private readonly IRevokeAccountRoleUseCase _revokeAccountRole;
    private readonly IGrantAccountPermissionUseCase _grantAccountPermission;
    private readonly IRevokeAccountPermissionUseCase _revokeAccountPermission;
    private readonly IDisableAccountUseCase _disableAccount;
    private readonly IEnableAccountUseCase _enableAccount;
    private readonly IResetAccountPasswordUseCase _resetAccountPassword;

    public AccountsAdministratorController(
        ICreateAccountUseCase createAccount,
        ISearchAccountsUseCase searchAccounts,
        IGetAccountByIdUseCase getAccountById,
        IGrantAccountRoleUseCase grantAccountRole,
        IRevokeAccountRoleUseCase revokeAccountRole,
        IGrantAccountPermissionUseCase grantAccountPermission,
        IRevokeAccountPermissionUseCase revokeAccountPermission,
        IDisableAccountUseCase disableAccount,
        IEnableAccountUseCase enableAccount,
        IResetAccountPasswordUseCase resetAccountPassword)
    {
        _createAccount = createAccount;
        _searchAccounts = searchAccounts;
        _getAccountById = getAccountById;
        _grantAccountRole = grantAccountRole;
        _revokeAccountRole = revokeAccountRole;
        _grantAccountPermission = grantAccountPermission;
        _revokeAccountPermission = revokeAccountPermission;
        _disableAccount = disableAccount;
        _enableAccount = enableAccount;
        _resetAccountPassword = resetAccountPassword;
    }

    [HttpPost]
    public async Task<ActionResult> CreateAccountAsync(
        [FromBody] CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createAccount.HandleAsync(
            new CreateAccountCommand(
                request.Username,
                request.Password,
                request.AccountType,
                Request.Headers[AdministratorCreateTokenHeader].FirstOrDefault()),
            cancellationToken);

        return ToOperationResult(result);
    }

    [HttpPost("search")]
    public async Task<ActionResult> SearchAccountsAsync(
        [FromBody] SearchAccountsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _searchAccounts.HandleAsync(
            new SearchAccountsQuery(request.Limit, request.Offset, request.Keyword, request.Status, request.Role),
            cancellationToken);

        return ToOperationResult(result);
    }

    [HttpGet("{accountId}")]
    public async Task<ActionResult> GetAccountByIdAsync(
        string accountId,
        CancellationToken cancellationToken)
    {
        var result = await _getAccountById.HandleAsync(
            new GetAccountByIdQuery(accountId),
            cancellationToken);

        return ToOperationResult(result);
    }

    [HttpPost("roles")]
    public async Task<ActionResult> GrantAccountRoleAsync(
        [FromBody] AccountRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _grantAccountRole.HandleAsync(
            new GrantAccountRoleCommand(request.AccountId, request.Role),
            cancellationToken);

        return ToOperationResult(result);
    }

    [HttpDelete("roles")]
    public async Task<ActionResult> RevokeAccountRoleAsync(
        [FromBody] AccountRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _revokeAccountRole.HandleAsync(
            new RevokeAccountRoleCommand(request.AccountId, request.Role),
            cancellationToken);

        return ToOperationResult(result);
    }

    [HttpPost("permissions")]
    public async Task<ActionResult> GrantAccountPermissionAsync(
        [FromBody] AccountPermissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _grantAccountPermission.HandleAsync(
            new GrantAccountPermissionCommand(request.AccountId, request.Permission),
            cancellationToken);

        return ToOperationResult(result);
    }

    [HttpDelete("permissions")]
    public async Task<ActionResult> RevokeAccountPermissionAsync(
        [FromBody] AccountPermissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _revokeAccountPermission.HandleAsync(
            new RevokeAccountPermissionCommand(request.AccountId, request.Permission),
            cancellationToken);

        return ToOperationResult(result);
    }

    [HttpPost("disable")]
    public async Task<ActionResult> DisableAccountAsync(
        [FromBody] AccountIdRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _disableAccount.HandleAsync(
            new DisableAccountCommand(request.AccountId),
            cancellationToken);

        return ToOperationResult(result);
    }

    [HttpPost("enable")]
    public async Task<ActionResult> EnableAccountAsync(
        [FromBody] AccountIdRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _enableAccount.HandleAsync(
            new EnableAccountCommand(request.AccountId),
            cancellationToken);

        return ToOperationResult(result);
    }

    [HttpPost("password")]
    public async Task<ActionResult> ResetAccountPasswordAsync(
        [FromBody] ResetAccountPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _resetAccountPassword.HandleAsync(
            new ResetAccountPasswordCommand(request.AccountId, request.NewPassword),
            cancellationToken);

        return ToOperationResult(result);
    }
}
