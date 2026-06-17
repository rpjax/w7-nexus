using Aidan.Core.Patterns;
using Nexus.Administrator.Application.Requests;
using Nexus.Administrator.Application.Responses;

namespace Nexus.Administrator.Application.Contracts;

public interface IAdministratorTeamOperatorCommandService
{
    Task<IResult<AssignOperatorToTeamResponse>> AssignOperatorToTeamAsync(AssignOperatorToTeamRequest request);

    Task<IResult<UnassignOperatorFromTeamResponse>> UnassignOperatorFromTeamAsync(
        UnassignOperatorFromTeamRequest request);

    Task<IResult<SetOperatorProfitShareRuleResponse>> SetOperatorProfitShareRuleAsync(
        SetOperatorProfitShareRuleRequest request);
}
