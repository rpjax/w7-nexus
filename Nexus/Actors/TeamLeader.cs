using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Actors.Contracts;
using Nexus.Actors.Requests;
using Nexus.Actors.Responses;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application;
using Nexus.Operations.ErrorCodes;

namespace Nexus.Actors;

public class TeamLeader : ITeamLeader
{
    private ITeamService _teamService { get; }

    public TeamLeader(ITeamService teamService)
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

    public async Task<IResult<SetTeamGatewaySelectionStrategyResponse>> SetTeamGatewaySelectionStrategyAsync(
        SetTeamGatewaySelectionStrategyRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<SetTeamGatewaySelectionStrategyResponse>();

        var result = await _teamService.SetGatewaySelectionStrategyAsync(request.TeamId, request.Strategy);
        return ToResponse<SetTeamGatewaySelectionStrategyResponse>(result);
    }

    public async Task<IResult<AssignStrawManToTeamResponse>> AssignStrawManToTeamAsync(
        AssignStrawManToTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<AssignStrawManToTeamResponse>();

        var result = await _teamService.AssignStrawManAsync(request.TeamId, request.StrawManId);
        return ToResponse<AssignStrawManToTeamResponse>(result);
    }

    public async Task<IResult<UnassignStrawManFromTeamResponse>> UnassignStrawManFromTeamAsync(
        UnassignStrawManFromTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<UnassignStrawManFromTeamResponse>();

        var result = await _teamService.UnassignStrawManAsync(request.TeamId, request.StrawManId);
        return ToResponse<UnassignStrawManFromTeamResponse>(result);
    }

    public async Task<IResult<AssignGatewayAccountGroupToTeamResponse>> AssignGatewayAccountGroupToTeamAsync(
        AssignGatewayAccountGroupToTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<AssignGatewayAccountGroupToTeamResponse>();

        var result = await _teamService.AssignGatewayCredentialsGroupAsync(
            request.TeamId,
            request.GatewayCredentialsGroupId);
        return ToResponse<AssignGatewayAccountGroupToTeamResponse>(result);
    }

    public async Task<IResult<UnassignGatewayAccountGroupFromTeamResponse>> UnassignGatewayAccountGroupFromTeamAsync(
        UnassignGatewayAccountGroupFromTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<UnassignGatewayAccountGroupFromTeamResponse>();

        var result = await _teamService.UnassignGatewayCredentialsGroupAsync(
            request.TeamId,
            request.GatewayCredentialsGroupId);
        return ToResponse<UnassignGatewayAccountGroupFromTeamResponse>(result);
    }

    public async Task<IResult<AssignGatewayAccountToTeamResponse>> AssignGatewayAccountToTeamAsync(
        AssignGatewayAccountToTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<AssignGatewayAccountToTeamResponse>();

        var result = await _teamService.AssignGatewayCredentialsAsync(
            request.TeamId,
            request.GatewayCredentialsId);
        return ToResponse<AssignGatewayAccountToTeamResponse>(result);
    }

    public async Task<IResult<UnassignGatewayAccountFromTeamResponse>> UnassignGatewayAccountFromTeamAsync(
        UnassignGatewayAccountFromTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<UnassignGatewayAccountFromTeamResponse>();

        var result = await _teamService.UnassignGatewayCredentialsAsync(
            request.TeamId,
            request.GatewayCredentialsId);
        return ToResponse<UnassignGatewayAccountFromTeamResponse>(result);
    }

    public async Task<IResult<SetOperatorProfitShareRuleResponse>> SetOperatorProfitShareRuleAsync(
        SetOperatorProfitShareRuleRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<SetOperatorProfitShareRuleResponse>();

        var cuts = (request.Cuts ?? Array.Empty<ProfitShareCutRequest>())
            .Select(cut => new ProfitSplit(
                string.IsNullOrWhiteSpace(cut.AccountId) ? string.Empty : cut.AccountId.Trim(),
                cut.Percentage))
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
            .WithMessage("Request body is required.")
            .Build());
    }
}
