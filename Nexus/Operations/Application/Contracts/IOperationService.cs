using Aidan.Core.Patterns;
using Nexus.Operations.Aggregates;

namespace Nexus.Operations.Application.Contracts;

public interface IOperationService
{
    Task<IResult<Operation>> CreateOperationAsync(string? name, string? description);
    Task<IResult> AssignAdministratorAsync(string operationId, string administratorId);
    Task<IResult> UnassignAdministratorAsync(string operationId, string administratorId);
    Task<IResult> DeleteOperationAsync(string operationId);
    Task<IResult> AssignStrawManAsync(string operationId, string strawManId);
    Task<IResult> UnassignStrawManAsync(string operationId, string strawManId);
    Task<IResult> SetGatewaySelectionStrategyAsync(string operationId, GatewaySelectionStrategy strategy);
    Task<IResult> AssignGatewayCredentialsGroupAsync(string operationId, string groupId);
    Task<IResult> UnassignGatewayCredentialsGroupAsync(string operationId, string groupId);
    Task<IResult> AssignGatewayCredentialsAsync(string operationId, string credentialsId);
    Task<IResult> UnassignGatewayCredentialsAsync(string operationId, string credentialsId);
}
