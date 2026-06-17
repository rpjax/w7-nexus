using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Accounts.Errors;
using Nexus.Operations.Application.Contracts;
using Nexus.TeamLeaders.Application.Contracts;
using Nexus.TeamLeaders.Application.Mapping;
using Nexus.TeamLeaders.Application.Requests;
using Nexus.TeamLeaders.Application.Responses;

namespace Nexus.TeamLeaders.Application.Services;

public sealed class TeamLeaderProfitShareAccountSearchService : ITeamLeaderProfitShareAccountSearchService
{
    private const int SearchKeywordMaxLength = 200;

    private ITeamRepository _teams { get; }
    private IAccountRepository _accounts { get; }

    public TeamLeaderProfitShareAccountSearchService(
        ITeamRepository teams,
        IAccountRepository accounts)
    {
        _teams = teams;
        _accounts = accounts;
    }

    public async Task<IResult<SearchProfitShareAccountsToAssignResponse>> SearchProfitShareAccountsToAssignAsync(
        SearchProfitShareAccountsToAssignRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<SearchProfitShareAccountsToAssignResponse>();

        var builder = Result.Create<SearchProfitShareAccountsToAssignResponse>();

        var limit = request.Limit <= 0 ? 20 : request.Limit;
        var offset = request.Offset;
        var keyword = request.Keyword?.Trim();

        if (limit < 1 || limit >= 1000)
        {
            builder.WithError(Error.Create()
                .WithCode(AccountErrorCodes.SearchLimitInvalid)
                .WithMessage("O limite deve estar entre 1 e 999.")
                .Build());
        }

        if (offset < 0)
        {
            builder.WithError(Error.Create()
                .WithCode(AccountErrorCodes.SearchOffsetInvalid)
                .WithMessage("O deslocamento não pode ser negativo.")
                .Build());
        }

        if (!string.IsNullOrWhiteSpace(keyword) && keyword.Length > SearchKeywordMaxLength)
        {
            builder.WithError(Error.Create()
                .WithCode(AccountErrorCodes.SearchKeywordTooLong)
                .WithMessage($"A palavra-chave pode ter no máximo {SearchKeywordMaxLength} caracteres.")
                .Build());
        }

        if (builder.ContainsError)
            return builder.Build();

        var scopeResult = await TeamLeaderOperationScope.ResolveAsync(_teams, request.TeamId);
        if (scopeResult.IsFailure)
            return Result<SearchProfitShareAccountsToAssignResponse>.Failure(scopeResult.Errors);

        var accountIds = TeamLeaderOperationScope.CollectOperationAccountIds(scopeResult.Value.OperationTeams).ToArray();

        if (accountIds.Length == 0)
        {
            return builder
                .WithValue(new SearchProfitShareAccountsToAssignResponse
                {
                    Offset = offset,
                    Limit = limit,
                    Total = 0,
                    Items = []
                })
                .Build();
        }

        var query = _accounts.AsQueryable()
            .Where(a => accountIds.Contains(a.Id));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.ToLowerInvariant();
            query = query.Where(a =>
                a.Id.ToLower().Contains(term) ||
                a.Username.ToLower().Contains(term));
        }

        var total = await query.CountAsync();

        var accounts = await query
            .OrderByDescending(a => a.LastUpdatedAt)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync();

        var response = new SearchProfitShareAccountsToAssignResponse
        {
            Offset = offset,
            Limit = limit,
            Total = total,
            Items = accounts
                .Select(a => a.ToAccountDetails())
                .ToList()
        };

        return builder
            .WithValue(response)
            .Build();
    }

    private static IResult<T> RequestBodyRequiredResult<T>()
    {
        return Result<T>.Failure(Error.Create()
            .WithCode(AccountErrorCodes.RequestBodyRequired)
            .WithMessage("O corpo da requisição é obrigatório.")
            .Build());
    }
}
