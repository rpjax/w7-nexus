using Aidan.Core.Patterns;
using Nexus.Operations.Application.Requests.Administrator;
using Nexus.Operations.Application.Responses.Administrator;

namespace Nexus.Operations.Application.Contracts;

public interface IAdministratorTeamOperatorCommandService
{
    Task<IResult<AssignOperatorToTeamResponse>> AssignOperatorToTeamAsync(AssignOperatorToTeamRequest request);

    Task<IResult<UnassignOperatorFromTeamResponse>> UnassignOperatorFromTeamAsync(
        UnassignOperatorFromTeamRequest request);

    Task<IResult<SetOperatorProfitShareRuleResponse>> SetOperatorProfitShareRuleAsync(
        SetOperatorProfitShareRuleRequest request);
}
