using Aidan.Core.Patterns;
using Nexus.Actors.Requests;
using Nexus.Actors.Responses;

namespace Nexus.Actors.Contracts;

public interface IOperationAdministrator
{
    Task<IResult<CreateOperationTeamResponse>> CreateOperationTeamAsync(
        CreateOperationTeamRequest request);

    Task<IResult<DeleteOperationTeamResponse>> DeleteOperationTeamAsync(
        DeleteOperationTeamRequest request);

    Task<IResult<AssignOperationTeamLeaderResponse>> AssignOperationTeamLeaderAsync(
        AssignOperationTeamLeaderRequest request);

    Task<IResult<UnassignOperationTeamLeaderResponse>> UnassignOperationTeamLeaderAsync(
        UnassignOperationTeamLeaderRequest request);
}
