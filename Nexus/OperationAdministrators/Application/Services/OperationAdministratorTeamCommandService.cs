using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.OperationAdministrators.Application.Contracts;
using Nexus.OperationAdministrators.Application.Mapping;
using Nexus.OperationAdministrators.Application.Requests;
using Nexus.OperationAdministrators.Application.Responses;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Errors;

namespace Nexus.OperationAdministrators.Application.Services;

public sealed class OperationAdministratorTeamCommandService : IOperationAdministratorTeamCommandService
{
    private ITeamService _teamService { get; }

    public OperationAdministratorTeamCommandService(ITeamService teamService)
    {
        _teamService = teamService;
    }

    public async Task<IResult<CreateOperationTeamResponse>> CreateOperationTeamAsync(
        CreateOperationTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<CreateOperationTeamResponse>();

        var result = await _teamService.CreateTeamAsync(request.OperationId, request.Name);
        if (result.IsFailure)
            return Result<CreateOperationTeamResponse>.Failure(result.Errors);

        if (result.Value is not Operations.Aggregates.Team team)
            return Result<CreateOperationTeamResponse>.Failure(result.Errors);

        return Result<CreateOperationTeamResponse>.Success(new CreateOperationTeamResponse
        {
            Team = TeamDetailsMapper.Map(team)
        });
    }

    public async Task<IResult<DeleteOperationTeamResponse>> DeleteOperationTeamAsync(
        DeleteOperationTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<DeleteOperationTeamResponse>();

        return ToResponse<DeleteOperationTeamResponse>(await _teamService.DeleteTeamAsync(request.TeamId));
    }

    public async Task<IResult<AssignOperationTeamLeaderResponse>> AssignOperationTeamLeaderAsync(
        AssignOperationTeamLeaderRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<AssignOperationTeamLeaderResponse>();

        return ToResponse<AssignOperationTeamLeaderResponse>(
            await _teamService.AssignTeamLeaderAsync(request.TeamId, request.TeamLeaderId));
    }

    public async Task<IResult<UnassignOperationTeamLeaderResponse>> UnassignOperationTeamLeaderAsync(
        UnassignOperationTeamLeaderRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<UnassignOperationTeamLeaderResponse>();

        return ToResponse<UnassignOperationTeamLeaderResponse>(
            await _teamService.UnassignTeamLeaderAsync(request.TeamId));
    }

    public async Task<IResult<SetTeamGatewaySelectionStrategyResponse>> SetTeamGatewaySelectionStrategyAsync(
        SetTeamGatewaySelectionStrategyRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<SetTeamGatewaySelectionStrategyResponse>();

        return ToResponse<SetTeamGatewaySelectionStrategyResponse>(
            await _teamService.SetGatewaySelectionStrategyAsync(request.TeamId, request.Strategy));
    }

    public async Task<IResult<AssignStrawManToTeamResponse>> AssignStrawManToTeamAsync(
        AssignStrawManToTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<AssignStrawManToTeamResponse>();

        return ToResponse<AssignStrawManToTeamResponse>(
            await _teamService.AssignStrawManAsync(request.TeamId, request.StrawManId));
    }

    public async Task<IResult<UnassignStrawManFromTeamResponse>> UnassignStrawManFromTeamAsync(
        UnassignStrawManFromTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<UnassignStrawManFromTeamResponse>();

        return ToResponse<UnassignStrawManFromTeamResponse>(
            await _teamService.UnassignStrawManAsync(request.TeamId, request.StrawManId));
    }

    public async Task<IResult<AssignGatewayAccountGroupToTeamResponse>> AssignGatewayAccountGroupToTeamAsync(
        AssignGatewayAccountGroupToTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<AssignGatewayAccountGroupToTeamResponse>();

        return ToResponse<AssignGatewayAccountGroupToTeamResponse>(
            await _teamService.AssignGatewayCredentialsGroupAsync(
                request.TeamId,
                request.GatewayCredentialsGroupId));
    }

    public async Task<IResult<UnassignGatewayAccountGroupFromTeamResponse>> UnassignGatewayAccountGroupFromTeamAsync(
        UnassignGatewayAccountGroupFromTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<UnassignGatewayAccountGroupFromTeamResponse>();

        return ToResponse<UnassignGatewayAccountGroupFromTeamResponse>(
            await _teamService.UnassignGatewayCredentialsGroupAsync(
                request.TeamId,
                request.GatewayCredentialsGroupId));
    }

    public async Task<IResult<AssignGatewayAccountToTeamResponse>> AssignGatewayAccountToTeamAsync(
        AssignGatewayAccountToTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<AssignGatewayAccountToTeamResponse>();

        return ToResponse<AssignGatewayAccountToTeamResponse>(
            await _teamService.AssignGatewayCredentialsAsync(
                request.TeamId,
                request.GatewayCredentialsId));
    }

    public async Task<IResult<UnassignGatewayAccountFromTeamResponse>> UnassignGatewayAccountFromTeamAsync(
        UnassignGatewayAccountFromTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<UnassignGatewayAccountFromTeamResponse>();

        return ToResponse<UnassignGatewayAccountFromTeamResponse>(
            await _teamService.UnassignGatewayCredentialsAsync(
                request.TeamId,
                request.GatewayCredentialsId));
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
