using Aidan.Core.Patterns;
using Nexus.Administrator.Application.Requests;
using Nexus.Administrator.Application.Responses;
using Nexus.Administrator.Application.Responses.Models;

namespace Nexus.Administrator.Application.Contracts;

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

    Task<IResult<SearchAccountsResponse>> SearchAccountsAsync(
        SearchAccountsRequest request);
}
