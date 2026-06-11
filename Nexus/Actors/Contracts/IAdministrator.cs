using Aidan.Core.Patterns;
using Nexus.Actors.Requests;
using Nexus.Actors.Responses;
using Nexus.Actors.Responses.Models;

namespace Nexus.Actors.Contracts;

public interface IAdministrator
{
    Task<IResult<OperationDetails>> CreateOperationAsync(
        CreateOperationRequest request);

    Task<IResult<SearchOperationsResponse>> SearchOperationsAsync(
        SearchOperationsRequest request);

    Task<IResult<DeleteOperationResponse>> DeleteOperationAsync(
        DeleteOperationRequest request);

    Task<IResult<AssignOperationAdministratorResponse>> AssignOperationAdministratorAsync(
        AssignOperationAdministratorRequest request);

    Task<IResult<UnassignOperationAdministratorResponse>> UnassignOperationAdministratorAsync(
        UnassignOperationAdministratorRequest request);
}
