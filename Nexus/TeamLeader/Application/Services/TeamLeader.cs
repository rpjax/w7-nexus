using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Microsoft.AspNetCore.Http;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Errors;
using Nexus.TeamLeader.Application.Contracts;
using Nexus.TeamLeader.Application.Requests;
using Nexus.TeamLeader.Application.Responses;
using Nexus.TeamLeader.Application.Responses.Models;
using Nexus.TeamLeader.Extensions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Nexus.TeamLeader.Application.Services;

public class TeamLeader : ITeamLeader
{
    private readonly string? _accountId;
    private ITeamService _teamService { get; }
    private IOperationRepository _operations { get; }
    private ITeamRepository _teams { get; }
    private IAccountRepository _accounts { get; }
    private IHttpContextAccessor? _httpContextAccessor { get; }

    public TeamLeader(
        ITeamService teamService,
        IOperationRepository operations,
        ITeamRepository teams,
        IAccountRepository accounts,
        IHttpContextAccessor httpContextAccessor)
    {
        _teamService = teamService;
        _operations = operations;
        _teams = teams;
        _accounts = accounts;
        _httpContextAccessor = httpContextAccessor;
    }

    internal TeamLeader(
        string accountId,
        ITeamService teamService,
        IOperationRepository operations,
        ITeamRepository teams,
        IAccountRepository accounts)
    {
        _accountId = accountId;
        _teamService = teamService;
        _operations = operations;
        _teams = teams;
        _accounts = accounts;
    }

    public async Task<IResult<SearchLedTeamsResponse>> SearchLedTeamsAsync(SearchLedTeamsRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<SearchLedTeamsResponse>();

        var accountId = await ResolveAccountIdAsync();
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Result<SearchLedTeamsResponse>.Failure(Error.Create()
                .WithCode(TeamErrorCodes.TeamLeaderInvalid)
                .WithMessage("A identidade do líder de equipe não foi encontrada.")
                .Build());
        }

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

    private async Task<string?> ResolveAccountIdAsync()
    {
        if (!string.IsNullOrWhiteSpace(_accountId))
            return _accountId.Trim();

        var user = _httpContextAccessor?.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var accountId = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(accountId))
            return null;

        var account = await _accounts.AsQueryable()
            .Where(a => a.Id == accountId)
            .FirstOrDefaultAsync();

        return account is null ? accountId.Trim() : accountId.Trim();
    }

    private static IResult<TResponse> ToResponse<TResponse>(Aidan.Core.Patterns.IResult result)
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
