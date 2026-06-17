using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;
using Nexus.Authorization.Application.Models;
using Nexus.OperationAdministrator.Application.Contracts;
using Nexus.OperationAdministrator.Application.Requests;
using Nexus.OperationAdministrator.Application.Responses;
using Nexus.OperationAdministrator.Application.Mapping;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Errors;

namespace Nexus.OperationAdministrator.Application.Services;

public class OperationAdministrator : IOperationAdministrator
{
    private IOperationAdministratorAccessPolicy _policy { get; }
    private ITeamService _teamService { get; }
    private IOperationRepository _operations { get; }
    private ITeamRepository _teams { get; }
    private IAccountRepository _accounts { get; }
    private ITeamGatewayDetailsLoader _teamGatewayDetailsLoader { get; }

    public OperationAdministrator(
        IOperationAdministratorAccessPolicy policy,
        ITeamService teamService,
        IOperationRepository operations,
        ITeamRepository teams,
        IAccountRepository accounts,
        ITeamGatewayDetailsLoader teamGatewayDetailsLoader)
    {
        _policy = policy;
        _teamService = teamService;
        _operations = operations;
        _teams = teams;
        _accounts = accounts;
        _teamGatewayDetailsLoader = teamGatewayDetailsLoader;
    }

    public Task<IOperationResult<SearchOperationsResponse>> SearchOperationsAsync(
        RequesterIdentity identity,
        SearchOperationsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeSearchOperationsAsync(identity, ct),
            () => SearchOperationsCoreAsync(identity, request),
            cancellationToken);
    }

    public Task<IOperationResult<CreateOperationTeamResponse>> CreateOperationTeamAsync(
        RequesterIdentity identity,
        CreateOperationTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, operationId: request?.OperationId ?? string.Empty, cancellationToken: ct),
            () => CreateOperationTeamCoreAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<DeleteOperationTeamResponse>> DeleteOperationTeamAsync(
        RequesterIdentity identity,
        DeleteOperationTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, teamId: request?.TeamId ?? string.Empty, cancellationToken: ct),
            () => DeleteOperationTeamCoreAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<AssignOperationTeamLeaderResponse>> AssignOperationTeamLeaderAsync(
        RequesterIdentity identity,
        AssignOperationTeamLeaderRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, teamId: request?.TeamId ?? string.Empty, cancellationToken: ct),
            () => AssignOperationTeamLeaderCoreAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<UnassignOperationTeamLeaderResponse>> UnassignOperationTeamLeaderAsync(
        RequesterIdentity identity,
        UnassignOperationTeamLeaderRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, teamId: request?.TeamId ?? string.Empty, cancellationToken: ct),
            () => UnassignOperationTeamLeaderCoreAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<SetTeamGatewaySelectionStrategyResponse>> SetTeamGatewaySelectionStrategyAsync(
        RequesterIdentity identity,
        SetTeamGatewaySelectionStrategyRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, teamId: request?.TeamId ?? string.Empty, cancellationToken: ct),
            () => SetTeamGatewaySelectionStrategyCoreAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<AssignStrawManToTeamResponse>> AssignStrawManToTeamAsync(
        RequesterIdentity identity,
        AssignStrawManToTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, teamId: request?.TeamId ?? string.Empty, cancellationToken: ct),
            () => AssignStrawManToTeamCoreAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<UnassignStrawManFromTeamResponse>> UnassignStrawManFromTeamAsync(
        RequesterIdentity identity,
        UnassignStrawManFromTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, teamId: request?.TeamId ?? string.Empty, cancellationToken: ct),
            () => UnassignStrawManFromTeamCoreAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<AssignGatewayAccountGroupToTeamResponse>> AssignGatewayAccountGroupToTeamAsync(
        RequesterIdentity identity,
        AssignGatewayAccountGroupToTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, teamId: request?.TeamId ?? string.Empty, cancellationToken: ct),
            () => AssignGatewayAccountGroupToTeamCoreAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<UnassignGatewayAccountGroupFromTeamResponse>> UnassignGatewayAccountGroupFromTeamAsync(
        RequesterIdentity identity,
        UnassignGatewayAccountGroupFromTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, teamId: request?.TeamId ?? string.Empty, cancellationToken: ct),
            () => UnassignGatewayAccountGroupFromTeamCoreAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<AssignGatewayAccountToTeamResponse>> AssignGatewayAccountToTeamAsync(
        RequesterIdentity identity,
        AssignGatewayAccountToTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, teamId: request?.TeamId ?? string.Empty, cancellationToken: ct),
            () => AssignGatewayAccountToTeamCoreAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<UnassignGatewayAccountFromTeamResponse>> UnassignGatewayAccountFromTeamAsync(
        RequesterIdentity identity,
        UnassignGatewayAccountFromTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, teamId: request?.TeamId ?? string.Empty, cancellationToken: ct),
            () => UnassignGatewayAccountFromTeamCoreAsync(request),
            cancellationToken);
    }

    private async Task<IOperationResult<T>> ExecuteAsync<T>(
        RequesterIdentity identity,
        Func<CancellationToken, Task<IAuthorizationResult>> authorizeAsync,
        Func<Task<IResult<T>>> executeAsync,
        CancellationToken cancellationToken)
    {
        var authorization = await authorizeAsync(cancellationToken);

        if (authorization.IsFailure)
            return OperationResult<T>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<T>.Unauthorized(authorization.AuthorizationErrors);

        var result = await executeAsync();

        if (result.IsFailure)
            return OperationResult<T>.Failure(result.Errors);

        if (result.Value is not T value)
            return OperationResult<T>.Failure(result.Errors);

        return OperationResult<T>.Success(value);
    }

    private async Task<IResult<SearchOperationsResponse>> SearchOperationsCoreAsync(
        RequesterIdentity identity,
        SearchOperationsRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<SearchOperationsResponse>();

        var builder = Result.Create<SearchOperationsResponse>();

        var limit = request.Limit <= 0 ? 20 : request.Limit;
        var offset = request.Offset;
        var keyword = request.Keyword?.Trim();

        if (limit < 1 || limit >= 1000)
        {
            builder.WithError(Error.Create()
                .WithCode(OperationErrorCodes.SearchLimitInvalid)
                .WithMessage("O limite deve estar entre 1 e 999.")
                .Build());
        }

        if (offset < 0)
        {
            builder.WithError(Error.Create()
                .WithCode(OperationErrorCodes.SearchOffsetInvalid)
                .WithMessage("O deslocamento não pode ser negativo.")
                .Build());
        }

        if (!string.IsNullOrWhiteSpace(keyword) && keyword.Length > Operation.MaxNameLength)
        {
            builder.WithError(Error.Create()
                .WithCode(OperationErrorCodes.SearchKeywordTooLong)
                .WithMessage($"A palavra-chave pode ter no máximo {Operation.MaxNameLength} caracteres.")
                .Build());
        }

        if (builder.ContainsError)
            return builder.Build();

        var query = _operations.AsQueryable();

        if (!RoleAuthorization.IsGlobalAdministrator(identity.Roles))
        {
            query = query.Where(o => o.AdministratorIds.Contains(identity.AccountId));
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.ToLowerInvariant();
            query = query.Where(o =>
                o.Id.ToLower().Contains(term) ||
                o.Name.ToLower().Contains(term) ||
                (o.Description != null && o.Description.ToLower().Contains(term)));
        }

        var total = await query.CountAsync();

        var operations = await query
            .OrderByDescending(o => o.UpdatedAt)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync();

        var items = await OperationDetailsMapper.MapManyAsync(
            operations,
            _teams,
            _accounts,
            _teamGatewayDetailsLoader);

        var response = new SearchOperationsResponse
        {
            Offset = offset,
            Limit = limit,
            Total = total,
            Items = items.ToList()
        };

        return builder
            .WithValue(response)
            .Build();
    }

    private async Task<IResult<CreateOperationTeamResponse>> CreateOperationTeamCoreAsync(
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

    private async Task<IResult<DeleteOperationTeamResponse>> DeleteOperationTeamCoreAsync(
        DeleteOperationTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<DeleteOperationTeamResponse>();

        return ToResponse<DeleteOperationTeamResponse>(await _teamService.DeleteTeamAsync(request.TeamId));
    }

    private async Task<IResult<AssignOperationTeamLeaderResponse>> AssignOperationTeamLeaderCoreAsync(
        AssignOperationTeamLeaderRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<AssignOperationTeamLeaderResponse>();

        return ToResponse<AssignOperationTeamLeaderResponse>(
            await _teamService.AssignTeamLeaderAsync(request.TeamId, request.TeamLeaderId));
    }

    private async Task<IResult<UnassignOperationTeamLeaderResponse>> UnassignOperationTeamLeaderCoreAsync(
        UnassignOperationTeamLeaderRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<UnassignOperationTeamLeaderResponse>();

        return ToResponse<UnassignOperationTeamLeaderResponse>(
            await _teamService.UnassignTeamLeaderAsync(request.TeamId));
    }

    private async Task<IResult<SetTeamGatewaySelectionStrategyResponse>> SetTeamGatewaySelectionStrategyCoreAsync(
        SetTeamGatewaySelectionStrategyRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<SetTeamGatewaySelectionStrategyResponse>();

        return ToResponse<SetTeamGatewaySelectionStrategyResponse>(
            await _teamService.SetGatewaySelectionStrategyAsync(request.TeamId, request.Strategy));
    }

    private async Task<IResult<AssignStrawManToTeamResponse>> AssignStrawManToTeamCoreAsync(
        AssignStrawManToTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<AssignStrawManToTeamResponse>();

        return ToResponse<AssignStrawManToTeamResponse>(
            await _teamService.AssignStrawManAsync(request.TeamId, request.StrawManId));
    }

    private async Task<IResult<UnassignStrawManFromTeamResponse>> UnassignStrawManFromTeamCoreAsync(
        UnassignStrawManFromTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<UnassignStrawManFromTeamResponse>();

        return ToResponse<UnassignStrawManFromTeamResponse>(
            await _teamService.UnassignStrawManAsync(request.TeamId, request.StrawManId));
    }

    private async Task<IResult<AssignGatewayAccountGroupToTeamResponse>> AssignGatewayAccountGroupToTeamCoreAsync(
        AssignGatewayAccountGroupToTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<AssignGatewayAccountGroupToTeamResponse>();

        return ToResponse<AssignGatewayAccountGroupToTeamResponse>(
            await _teamService.AssignGatewayCredentialsGroupAsync(
                request.TeamId,
                request.GatewayCredentialsGroupId));
    }

    private async Task<IResult<UnassignGatewayAccountGroupFromTeamResponse>> UnassignGatewayAccountGroupFromTeamCoreAsync(
        UnassignGatewayAccountGroupFromTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<UnassignGatewayAccountGroupFromTeamResponse>();

        return ToResponse<UnassignGatewayAccountGroupFromTeamResponse>(
            await _teamService.UnassignGatewayCredentialsGroupAsync(
                request.TeamId,
                request.GatewayCredentialsGroupId));
    }

    private async Task<IResult<AssignGatewayAccountToTeamResponse>> AssignGatewayAccountToTeamCoreAsync(
        AssignGatewayAccountToTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<AssignGatewayAccountToTeamResponse>();

        return ToResponse<AssignGatewayAccountToTeamResponse>(
            await _teamService.AssignGatewayCredentialsAsync(
                request.TeamId,
                request.GatewayCredentialsId));
    }

    private async Task<IResult<UnassignGatewayAccountFromTeamResponse>> UnassignGatewayAccountFromTeamCoreAsync(
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
