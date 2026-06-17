using Aidan.Core.Patterns;
using Nexus.Administrators.Application.Requests;
using Nexus.Administrators.Application.Responses;
using Nexus.Administrators.Application.Responses.Models;

namespace Nexus.Administrators.Application.Contracts;

public interface IAdministratorOperationCommandService
{
    Task<IResult<OperationDetails>> CreateOperationAsync(CreateOperationRequest request);

    Task<IResult<DeleteOperationResponse>> DeleteOperationAsync(DeleteOperationRequest request);

    Task<IResult<AssignOperationAdministratorResponse>> AssignOperationAdministratorAsync(
        AssignOperationAdministratorRequest request);

    Task<IResult<UnassignOperationAdministratorResponse>> UnassignOperationAdministratorAsync(
        UnassignOperationAdministratorRequest request);
}
