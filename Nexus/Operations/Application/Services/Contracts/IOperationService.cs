using Aidan.Core.Patterns;
using Nexus.Operations.Aggregates;

namespace Nexus.Operations.Application.Services.Contracts;

public interface IOperationService
{
    Task<IResult<Operation>> CreateOperationAsync(string? name, string? description);
    Task<IResult> AssignAdministratorAsync(string operationId, string administratorId);
    Task<IResult> UnassignAdministratorAsync(string operationId, string administratorId);
    Task<IResult> DeleteOperationAsync(string operationId);
}
