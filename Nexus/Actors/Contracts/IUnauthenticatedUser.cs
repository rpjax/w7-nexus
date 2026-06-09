using Aidan.Core.Patterns;
using Nexus.Actors.Requests;
using Nexus.Actors.Responses;

namespace Nexus.Actors.Contracts;

public interface IUnauthenticatedUser
{
    Task<IResult<CreateAdministratorAccountResponse>> CreateAdministratorAccountAsync(
        CreateAdministratorAccountRequest request);
    Task<IResult<CreateOperatorAccountResponse>> CreateOperatorAccountAsync(
        CreateOperatorAccountRequest request);
}
