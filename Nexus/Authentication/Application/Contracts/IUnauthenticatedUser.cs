using Aidan.Core.Patterns;
using Nexus.Authentication.Application.Requests;
using Nexus.Authentication.Application.Responses;

namespace Nexus.Authentication.Application.Contracts;

public interface IUnauthenticatedUser
{
    Task<IResult<CreateAdministratorAccountResponse>> CreateAdministratorAccountAsync(
        CreateAdministratorAccountRequest request);

    Task<IResult<CreateOperatorAccountResponse>> CreateOperatorAccountAsync(
        CreateOperatorAccountRequest request);

    Task<IResult<CreateStrawManAccountResponse>> CreateStrawManAccountAsync(
        CreateStrawManAccountRequest request);
}
