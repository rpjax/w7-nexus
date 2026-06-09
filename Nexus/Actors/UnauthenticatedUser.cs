using Aidan.Core.Patterns;
using Nexus.Actors.Contracts;
using Nexus.Actors.Requests;
using Nexus.Actors.Responses;

namespace Nexus.Actors;

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
