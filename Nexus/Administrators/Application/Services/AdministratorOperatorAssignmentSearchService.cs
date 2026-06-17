using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Accounts.Errors;
using Nexus.Administrators.Application.Contracts;
using Nexus.Administrators.Application.Mapping;
using Nexus.Administrators.Application.Requests;
using Nexus.Administrators.Application.Responses;
using Nexus.Authorizations;

namespace Nexus.Administrators.Application.Services;

public sealed class AdministratorOperatorAssignmentSearchService : IAdministratorOperatorAssignmentSearchService
{
    private const int SearchKeywordMaxLength = 200;

    private IAccountRepository _accounts { get; }

    public AdministratorOperatorAssignmentSearchService(IAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public async Task<IResult<SearchOperatorsToAssignResponse>> SearchOperatorsToAssignAsync(
        SearchOperatorsToAssignRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<SearchOperatorsToAssignResponse>();

        var builder = Result.Create<SearchOperatorsToAssignResponse>();

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

        var query = _accounts.AsQueryable()
            .Where(a => a.Roles.Contains(Roles.Operator));

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

        var response = new SearchOperatorsToAssignResponse
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
