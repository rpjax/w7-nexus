using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authentication.Application.Contracts;
using Nexus.Authentication.Application.Requests;
using Nexus.Authentication.Application.Responses;
using Nexus.Authorization;

namespace Nexus.Authentication.Application.Services;

public class UnauthenticatedUser : IUnauthenticatedUser
{
    private IAccountCreator _accountCreator { get; }

    public UnauthenticatedUser(IAccountCreator accountCreator)
    {
        _accountCreator = accountCreator;
    }

    public async Task<IResult<CreateAdministratorAccountResponse>> CreateAdministratorAccountAsync(
        CreateAdministratorAccountRequest request)
    {
        var roles = new[] { Roles.Administrator };
        var permissions = new string[0];

        var createAccountResult = await _accountCreator.CreateAccountAsync(
            username: request.Username,
            password: request.Password,
            roles: roles,
            permissions: permissions);

        if (createAccountResult.IsFailure)
            return Result<CreateAdministratorAccountResponse>.Failure(createAccountResult.Errors);

        return Result<CreateAdministratorAccountResponse>.Success(new CreateAdministratorAccountResponse());
    }

    public async Task<IResult<CreateOperatorAccountResponse>> CreateOperatorAccountAsync(
        CreateOperatorAccountRequest request)
    {
        var roles = new[] { Roles.Operator };
        var permissions = new string[0];

        var createAccountResult = await _accountCreator.CreateAccountAsync(
            username: request.Username,
            password: request.Password,
            roles: roles,
            permissions: permissions);

        if (createAccountResult.IsFailure)
            return Result<CreateOperatorAccountResponse>.Failure(createAccountResult.Errors);

        return Result<CreateOperatorAccountResponse>.Success(new CreateOperatorAccountResponse());
    }
}
