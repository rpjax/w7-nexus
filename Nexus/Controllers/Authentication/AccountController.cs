using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Aidan.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Nexus.Accounts.Aggregates;
using Nexus.Controllers.Authentication.Requests;
using Nexus.Accounts.Errors;
using Nexus.Accounts.Application.Services.Contracts;

namespace Nexus.Controllers.Authentication;

[Route("api/account")]
public class AccountController : WebController
{
    private IAccountUpdater _accountUpdater { get; }
    private IAccountRepository _accountRepository { get; }

    public AccountController(
        IAccountUpdater accountUpdater,
        IAccountRepository accountRepository)
    {
        _accountUpdater = accountUpdater;
        _accountRepository = accountRepository;
    }

    private static object ToAccountResponse(Account account)
    {
        return new
        {
            Id = account.Id,
            Username = account.Username,
            Roles = account.Roles,
            Permissions = account.Permissions,
            CreatedAt = account.CreatedAt,
            LastUpdatedAt = account.LastUpdatedAt,
        };
    }

    [HttpPost("search")]
    public async Task<ActionResult> GetAccounts([FromBody] SearchAccountsRequest? request)
    {
        request ??= new SearchAccountsRequest();

        var limit = request.Limit <= 0 ? 30 : request.Limit;
        var offset = request.Offset;
        var keyword = request.Keyword?.Trim();

        if (limit < 0 || limit >= 1000)
        {
            return ProblemResponse(422, Error.Create()
                .WithCode("Account.SEARCH_LIMIT_INVALID")
                .WithMessage("O limite deve estar entre 1 e 999.")
                .Build());
        }

        if (offset < 0)
        {
            return ProblemResponse(422, Error.Create()
                .WithCode("Account.SEARCH_OFFSET_INVALID")
                .WithMessage("O deslocamento não pode ser negativo.")
                .Build());
        }

        if (!string.IsNullOrWhiteSpace(keyword) && keyword.Length > 200)
        {
            return ProblemResponse(422, Error.Create()
                .WithCode("Account.SEARCH_KEYWORD_TOO_LONG")
                .WithMessage("A palavra-chave pode ter no máximo 200 caracteres.")
                .Build());
        }

        var query = _accountRepository.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.ToLowerInvariant();
            query = query.Where(a =>
                a.Id.ToLower().Contains(term)
                || a.Username.ToLower().Contains(term)
            );
        }

        var total = await query.CountAsync();

        var accounts = await query
            .OrderByDescending(a => a.LastUpdatedAt)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync();

        var items = accounts
            .Select(ToAccountResponse)
            .ToArray();

        return Ok(new
        {
            Total = total,
            Items = items,
        });
    }

    [HttpPatch("username")]
    public async Task<ActionResult> ChangeUsernameAsync([FromBody] ChangeUsernameRequest request)
    {
        var result = await _accountUpdater.UpdateUsernameAsync(request.AccountId, request.NewUsername);
        return ToUpdaterActionResult(result);
    }

    [HttpPatch("password")]
    public async Task<ActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequest request)
    {
        var result = await _accountUpdater.UpdatePasswordAsync(request.AccountId, request.NewPassword);
        return ToUpdaterActionResult(result);
    }

    [HttpPost("roles")]
    public async Task<ActionResult> AddRoleAsync([FromBody] AddRoleRequest request)
    {
        var result = await _accountUpdater.AddRoleAsync(request.AccountId, request.Role);
        return ToUpdaterActionResult(result);
    }

    [HttpDelete("roles")]
    public async Task<ActionResult> RemoveRoleAsync([FromBody] RemoveRoleRequest request)
    {
        var result = await _accountUpdater.RemoveRoleAsync(request.AccountId, request.Role);
        return ToUpdaterActionResult(result);
    }

    [HttpDelete("roles/all")]
    public async Task<ActionResult> ClearRolesAsync([FromBody] ClearRolesRequest request)
    {
        var result = await _accountUpdater.ClearRolesAsync(request.AccountId);
        return ToUpdaterActionResult(result);
    }

    [HttpPost("permissions")]
    public async Task<ActionResult> AddPermissionAsync([FromBody] AddPermissionRequest request)
    {
        var result = await _accountUpdater.AddPermissionAsync(request.AccountId, request.Permission);
        return ToUpdaterActionResult(result);
    }

    [HttpDelete("permissions")]
    public async Task<ActionResult> RemovePermissionAsync([FromBody] RemovePermissionRequest request)
    {
        var result = await _accountUpdater.RemovePermissionAsync(request.AccountId, request.Permission);
        return ToUpdaterActionResult(result);
    }

    [HttpDelete("permissions/all")]
    public async Task<ActionResult> ClearPermissionsAsync([FromBody] ClearPermissionsRequest request)
    {
        var result = await _accountUpdater.ClearPermissionsAsync(request.AccountId);
        return ToUpdaterActionResult(result);
    }

    private ActionResult ToUpdaterActionResult(IResult result)
    {
        if (result.IsSuccess)
            return NoContent();

        if (result.Errors.Any(e => e.Code == AccountErrorCodes.AccountNotFound))
            return ProblemResponse(404, result.Errors);

        return ProblemResponse(422, result.Errors);
    }

}
