using Aidan.Core.Patterns;
using Nexus.Operators.Actors.Contracts;
using Nexus.Operators.Actors.Requests;
using Nexus.Operators.Actors.Responses;

namespace Nexus.Operators.Actors;

public class UnauthenticatedUser : IUnauthenticatedUser
{
    public Task<IResult<CreateAdministratorAccountResponse>> CreateAdministratorAccountAsync(
        CreateAdministratorAccountRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<IResult<CreateOperatorAccountResponse>> CreateOperatorAccountAsync(
        CreateOperatorAccountRequest request)
    {
        throw new NotImplementedException();
    }
}
