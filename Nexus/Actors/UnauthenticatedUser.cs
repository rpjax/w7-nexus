using Aidan.Core.Patterns;
using Nexus.Accounts.Application;
using Nexus.Actors.Contracts;
using Nexus.Actors.Requests;
using Nexus.Actors.Responses;
using Nexus.Authorization;

namespace Nexus.Actors;

public class UnauthenticatedUser : IUnauthenticatedUser
{
    private IAccountCreator _accountCreator { get; }

    public UnauthenticatedUser(
        IAccountCreator accountCreator)
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
        {
            return Result<CreateAdministratorAccountResponse>.Failure(createAccountResult.Errors);
        }

        var response = new CreateAdministratorAccountResponse();

        return Result<CreateAdministratorAccountResponse>.Success(response);
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
        {
            return Result<CreateOperatorAccountResponse>.Failure(createAccountResult.Errors);
        }

        var response = new CreateOperatorAccountResponse();

        return Result<CreateOperatorAccountResponse>.Success(response);
    }
}
