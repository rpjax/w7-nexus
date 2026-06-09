using Aidan.Core.Patterns;
using Nexus.Operators.Actors.Requests;
using Nexus.Operators.Actors.Responses;

namespace Nexus.Operators.Actors.Contracts;

public interface IUnauthenticatedUser
{
    Task<IResult<CreateAdministratorAccountResponse>> CreateAdministratorAccountAsync(
        CreateAdministratorAccountRequest request);
    Task<IResult<CreateOperatorAccountResponse>> CreateOperatorAccountAsync(
        CreateOperatorAccountRequest request);
}
