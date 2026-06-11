using Aidan.Core.Patterns;
using Nexus.Operations.Application.Contracts;
using Nexus.Actors.Responses.Models;

namespace Nexus.Operations.Application.Contracts;

public interface IOperationService
{
    Task<IResult<OperationDetails>> CreateOperationAsync(string? name, string? description);

    Task<IResult> AssignAdministratorAsync(string operationId, string administratorId);
    Task<IResult> UnassignAdministratorAsync(string operationId, string administratorId);
    Task<IResult> DeleteOperationAsync(string operationId);
}
