using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization.Application.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Errors;
using Nexus.TeamLeader.Application.Contracts;
using Nexus.TeamLeader.Application.Requests;
using Nexus.TeamLeader.Application.Responses;
using Nexus.TeamLeader.Application.Responses.Models;
using Nexus.TeamLeader.Extensions;

namespace Nexus.TeamLeader.Application.Services;

public class TeamLeader : ITeamLeader
{
    private ITeamLeaderAccessPolicy _policy { get; }
    private ITeamService _teamService { get; }
    private IOperationRepository _operations { get; }
    private ITeamRepository _teams { get; }
    private IAccountRepository _accounts { get; }

    public TeamLeader(
        ITeamLeaderAccessPolicy policy,
        ITeamService teamService,
        IOperationRepository operations,
        ITeamRepository teams,
        IAccountRepository accounts)
    {
        _policy = policy;
        _teamService = teamService;
        _operations = operations;
        _teams = teams;
        _accounts = accounts;
    }

    public Task<IOperationResult<SearchLedTeamsResponse>> SearchLedTeamsAsync(
        RequesterIdentity identity,
        SearchLedTeamsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeSearchLedTeamsAsync(identity),
            () => SearchLedTeamsCoreAsync(identity, request),
            cancellationToken);
    }

    public Task<IOperationResult<AssignOperatorToTeamResponse>> AssignOperatorToTeamAsync(
        RequesterIdentity identity,
        AssignOperatorToTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeManageTeamAsync(identity, teamId: request?.TeamId ?? string.Empty),
            () => AssignOperatorToTeamCoreAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<UnassignOperatorFromTeamResponse>> UnassignOperatorFromTeamAsync(
        RequesterIdentity identity,
        UnassignOperatorFromTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeManageTeamAsync(identity, teamId: request?.TeamId ?? string.Empty),
            () => UnassignOperatorFromTeamCoreAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<SetOperatorProfitShareRuleResponse>> SetOperatorProfitShareRuleAsync(
        RequesterIdentity identity,
        SetOperatorProfitShareRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeManageTeamAsync(identity, teamId: request?.TeamId ?? string.Empty),
            () => SetOperatorProfitShareRuleCoreAsync(request),
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

    private async Task<IResult<SearchLedTeamsResponse>> SearchLedTeamsCoreAsync(
        RequesterIdentity identity,
        SearchLedTeamsRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<SearchLedTeamsResponse>();

        var accountId = identity.AccountId;

        var builder = Result.Create<SearchLedTeamsResponse>();

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

        var ledTeams = await _teams.AsQueryable()
            .Where(t => t.TeamLeaderId == accountId)
            .ToArrayAsync();

        if (ledTeams.Length == 0)
        {
            return builder
                .WithValue(new SearchLedTeamsResponse
                {
                    Offset = offset,
                    Limit = limit,
                    Total = 0,
                    Items = new List<OperationWithLedTeamsDetails>()
                })
                .Build();
        }

        var operationIds = ledTeams
            .Select(t => t.OperationId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var query = _operations.AsQueryable()
            .Where(o => operationIds.Contains(o.Id));

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

        var pageOperationIds = operations.Select(o => o.Id).ToHashSet(StringComparer.Ordinal);
        var pageLedTeams = ledTeams
            .Where(t => pageOperationIds.Contains(t.OperationId))
            .ToArray();

        var items = await OperationWithLedTeamsDetailsMapper.MapManyAsync(
            operations,
            pageLedTeams,
            _accounts);

        var response = new SearchLedTeamsResponse
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

    private async Task<IResult<AssignOperatorToTeamResponse>> AssignOperatorToTeamCoreAsync(
        AssignOperatorToTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<AssignOperatorToTeamResponse>();

        var result = await _teamService.AssignOperatorAsync(request.TeamId, request.OperatorId);
        return ToResponse<AssignOperatorToTeamResponse>(result);
    }

    private async Task<IResult<UnassignOperatorFromTeamResponse>> UnassignOperatorFromTeamCoreAsync(
        UnassignOperatorFromTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<UnassignOperatorFromTeamResponse>();

        var result = await _teamService.UnassignOperatorAsync(request.TeamId, request.OperatorId);
        return ToResponse<UnassignOperatorFromTeamResponse>(result);
    }

    private async Task<IResult<SetOperatorProfitShareRuleResponse>> SetOperatorProfitShareRuleCoreAsync(
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
