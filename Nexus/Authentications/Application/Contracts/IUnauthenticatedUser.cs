using Aidan.Core.Patterns;
using Nexus.Authentications.Application.Requests;
using Nexus.Authentications.Application.Responses;

namespace Nexus.Authentications.Application.Contracts;

public interface IUnauthenticatedUser
{
    Task<IResult<CreateAdministratorAccountResponse>> CreateAdministratorAccountAsync(
        CreateAdministratorAccountRequest request);

    Task<IResult<CreateOperatorAccountResponse>> CreateOperatorAccountAsync(
        CreateOperatorAccountRequest request);

    Task<IResult<CreateStrawManAccountResponse>> CreateStrawManAccountAsync(
        CreateStrawManAccountRequest request);
}
