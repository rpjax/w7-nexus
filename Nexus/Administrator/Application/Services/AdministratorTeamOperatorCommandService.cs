using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Administrator.Application.Contracts;
using Nexus.Administrator.Application.Requests;
using Nexus.Administrator.Application.Responses;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Errors;

namespace Nexus.Administrator.Application.Services;

public sealed class AdministratorTeamOperatorCommandService : IAdministratorTeamOperatorCommandService
{
    private ITeamService _teamService { get; }

    public AdministratorTeamOperatorCommandService(ITeamService teamService)
    {
        _teamService = teamService;
    }

    public async Task<IResult<AssignOperatorToTeamResponse>> AssignOperatorToTeamAsync(
        AssignOperatorToTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<AssignOperatorToTeamResponse>();

        var result = await _teamService.AssignOperatorAsync(request.TeamId, request.OperatorId);
        return ToResponse<AssignOperatorToTeamResponse>(result);
    }

    public async Task<IResult<UnassignOperatorFromTeamResponse>> UnassignOperatorFromTeamAsync(
        UnassignOperatorFromTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<UnassignOperatorFromTeamResponse>();

        var result = await _teamService.UnassignOperatorAsync(request.TeamId, request.OperatorId);
        return ToResponse<UnassignOperatorFromTeamResponse>(result);
    }

    public async Task<IResult<SetOperatorProfitShareRuleResponse>> SetOperatorProfitShareRuleAsync(
        SetOperatorProfitShareRuleRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<SetOperatorProfitShareRuleResponse>();

        var cuts = request.Cuts
            .Select(cut => new ProfitSplit(cut.AccountId.Trim(), cut.Percentage))
            .ToList();

        var result = await _teamService.SetOperatorProfitShareRuleAsync(
            request.TeamId,
            request.OperatorId,
            cuts);
        return ToResponse<SetOperatorProfitShareRuleResponse>(result);
    }

    private static IResult<TResponse> ToResponse<TResponse>(IResult result)
        where TResponse : new()
    {
        if (result.IsFailure)
            return Result<TResponse>.Failure(result.Errors);

        return Result<TResponse>.Success(new TResponse());
    }

    private static IResult<T> RequestBodyRequiredResult<T>()
    {
        return Result<T>.Failure(Error.Create()
            .WithCode(OperationErrorCodes.RequestBodyRequired)
            .WithMessage("O corpo da requisição é obrigatório.")
            .Build());
    }
}
