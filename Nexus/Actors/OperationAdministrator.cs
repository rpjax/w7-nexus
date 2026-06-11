using Aidan.Core.Errors;
using Nexus.Operations.Application.Contracts;
using Aidan.Core.Patterns;
using Nexus.Actors.Contracts;
using Nexus.Actors.Requests;
using Nexus.Actors.Responses;
using Nexus.Operations.Application;
using Nexus.Operations.Errors;

namespace Nexus.Actors;

public class OperationAdministrator : IOperationAdministrator
{
    private ITeamService _teamService { get; }

    public OperationAdministrator(ITeamService teamService)
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

        return Result<CreateOperationTeamResponse>.Success(new CreateOperationTeamResponse
        {
            Team = result.Value!
        });
    }

    public async Task<IResult<DeleteOperationTeamResponse>> DeleteOperationTeamAsync(
        DeleteOperationTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<DeleteOperationTeamResponse>();

        var result = await _teamService.DeleteTeamAsync(request.TeamId);
        return ToResponse<DeleteOperationTeamResponse>(result);
    }

    public async Task<IResult<AssignOperationTeamLeaderResponse>> AssignOperationTeamLeaderAsync(
        AssignOperationTeamLeaderRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<AssignOperationTeamLeaderResponse>();

        var result = await _teamService.AssignTeamLeaderAsync(request.TeamId, request.TeamLeaderId);
        return ToResponse<AssignOperationTeamLeaderResponse>(result);
    }

    public async Task<IResult<UnassignOperationTeamLeaderResponse>> UnassignOperationTeamLeaderAsync(
        UnassignOperationTeamLeaderRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<UnassignOperationTeamLeaderResponse>();

        var result = await _teamService.UnassignTeamLeaderAsync(request.TeamId);
        return ToResponse<UnassignOperationTeamLeaderResponse>(result);
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
