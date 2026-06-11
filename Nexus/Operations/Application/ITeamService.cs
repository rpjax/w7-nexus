using Aidan.Core.Patterns;
using Nexus.Actors.Responses.Models;
using Nexus.Operations.Aggregates;

namespace Nexus.Operations.Application;

public interface ITeamService
{
    Task<IResult<TeamDetails>> CreateTeamAsync(string operationId, string? name);
    Task<IResult> DeleteTeamAsync(string teamId);
    Task<IResult> AssignTeamLeaderAsync(string teamId, string teamLeaderId);
    Task<IResult> UnassignTeamLeaderAsync(string teamId);
    Task<IResult> AssignOperatorAsync(string teamId, string operatorId);
    Task<IResult> UnassignOperatorAsync(string teamId, string operatorId);
    Task<IResult> AssignStrawManAsync(string teamId, string strawManId);
    Task<IResult> UnassignStrawManAsync(string teamId, string strawManId);
    Task<IResult> SetGatewaySelectionStrategyAsync(string teamId, GatewaySelectionStrategy strategy);
    Task<IResult> AssignGatewayCredentialsGroupAsync(string teamId, string groupId);
    Task<IResult> UnassignGatewayCredentialsGroupAsync(string teamId, string groupId);
    Task<IResult> AssignGatewayCredentialsAsync(string teamId, string credentialsId);
    Task<IResult> UnassignGatewayCredentialsAsync(string teamId, string credentialsId);
    Task<IResult> SetOperatorProfitShareRuleAsync(
        string teamId,
        string operatorId,
        IReadOnlyList<ProfitSplit> cuts);
}
