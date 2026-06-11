using Aidan.Core.Patterns;
using Nexus.Actors.Responses.Models;
using Nexus.Operations.Aggregates;

namespace Nexus.Operations.Application;

public interface IOperationService
{
    Task<IResult<OperationDetails>> CreateOperationAsync(
        string? name,
        string? description,
        string[] operatorIds);

    Task<IResult> AssignAdministratorAsync(string operationId, string administratorId);
    Task<IResult> UnassignAdministratorAsync(string operationId, string administratorId);
    Task<IResult> AssignOperatorAsync(string operationId, string operatorId);
    Task<IResult> UnassignOperatorAsync(string operationId, string operatorId);
    Task<IResult> AssignStrawManAsync(string operationId, string strawManId);
    Task<IResult> UnassignStrawManAsync(string operationId, string strawManId);
    Task<IResult> SetGatewaySelectionStrategyAsync(string operationId, OperationGatewaySelectionStrategy strategy);
    Task<IResult> AssignGatewayCredentialsGroupAsync(string operationId, string groupId);
    Task<IResult> UnassignGatewayCredentialsGroupAsync(string operationId, string groupId);
    Task<IResult> AssignGatewayCredentialsAsync(string operationId, string credentialsId);
    Task<IResult> UnassignGatewayCredentialsAsync(string operationId, string credentialsId);
    Task<IResult> DeleteOperationAsync(string operationId);
    Task<IResult> CreateTeamAsync(string operationId, string name);
}
