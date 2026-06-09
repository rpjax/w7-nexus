using Aidan.Core.Patterns;
using Nexus.Operators.Actors.Requests;
using Nexus.Operators.Actors.Responses;

namespace Nexus.Operators.Actors.Contracts;

public interface IAdministrator
{
    Task<IResult<CreateOperationResponse>> CreateOperationAsync(
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
