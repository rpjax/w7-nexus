using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Accounts.Errors;
using Nexus.Administrators.Application.Contracts;
using Nexus.Administrators.Application.Requests;
using Nexus.Administrators.Application.Responses;

namespace Nexus.Administrators.Application.Services;

public sealed class AdministratorAccountCommandService : IAdministratorAccountCommandService
{
    private IAccountUpdater _accountUpdater { get; }

    public AdministratorAccountCommandService(IAccountUpdater accountUpdater)
    {
        _accountUpdater = accountUpdater;
    }

    public async Task<IResult<GrantAccountRoleResponse>> GrantAccountRoleAsync(GrantAccountRoleRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<GrantAccountRoleResponse>();

        var result = await _accountUpdater.AddRoleAsync(request.AccountId, request.Role);
        return ToResponse<GrantAccountRoleResponse>(result);
    }

    public async Task<IResult<RevokeAccountRoleResponse>> RevokeAccountRoleAsync(RevokeAccountRoleRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<RevokeAccountRoleResponse>();

        var result = await _accountUpdater.RemoveRoleAsync(request.AccountId, request.Role);
        return ToResponse<RevokeAccountRoleResponse>(result);
    }

    public async Task<IResult<GrantAccountPermissionResponse>> GrantAccountPermissionAsync(
        GrantAccountPermissionRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<GrantAccountPermissionResponse>();

        var result = await _accountUpdater.AddPermissionAsync(request.AccountId, request.Permission);
        return ToResponse<GrantAccountPermissionResponse>(result);
    }

    public async Task<IResult<RevokeAccountPermissionResponse>> RevokeAccountPermissionAsync(
        RevokeAccountPermissionRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<RevokeAccountPermissionResponse>();

        var result = await _accountUpdater.RemovePermissionAsync(request.AccountId, request.Permission);
        return ToResponse<RevokeAccountPermissionResponse>(result);
    }

    private static IResult<T> ToResponse<T>(IResult result) where T : new()
    {
        if (result.IsFailure)
            return Result<T>.Failure(result.Errors);

        return Result.Create<T>().WithValue(new T()).Build();
    }

    private static IResult<T> RequestBodyRequiredResult<T>()
    {
        return Result<T>.Failure(Error.Create()
            .WithCode(AccountErrorCodes.RequestBodyRequired)
            .WithMessage("O corpo da requisição é obrigatório.")
            .Build());
    }
}
