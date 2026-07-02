using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization.Application.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Errors;
using Nexus.Operations.Application.Mapping;
using Nexus.Operations.Application.Requests.TeamLeader;
using Nexus.Operations.Application.Responses.TeamLeader;
using Nexus.Operations.Application.Responses.TeamLeader.Models;

namespace Nexus.Operations.Application.Services;

public sealed class TeamLeaderLedTeamsSearchService : ITeamLeaderLedTeamsSearchService
{
    private IOperationRepository _operations { get; }
    private ITeamRepository _teams { get; }
    private IAccountRepository _accounts { get; }

    public TeamLeaderLedTeamsSearchService(
        IOperationRepository operations,
        ITeamRepository teams,
        IAccountRepository accounts)
    {
        _operations = operations;
        _teams = teams;
        _accounts = accounts;
    }

    public async Task<IResult<SearchLedTeamsResponse>> SearchLedTeamsAsync(
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

        var items = await TeamLeaderOperationWithLedTeamsDetailsMapper.MapManyAsync(
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

    private static IResult<T> RequestBodyRequiredResult<T>()
    {
        return Result<T>.Failure(Error.Create()
            .WithCode(OperationErrorCodes.RequestBodyRequired)
            .WithMessage("O corpo da requisição é obrigatório.")
            .Build());
    }
}
