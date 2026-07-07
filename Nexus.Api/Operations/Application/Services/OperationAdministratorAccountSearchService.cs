using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Accounts.Errors;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Application.Mapping;
using Nexus.Operations.Application.Requests.OperationAdministrator;
using Nexus.Operations.Application.Responses.OperationAdministrator;

namespace Nexus.Operations.Application.Services;

public sealed class OperationAdministratorAccountSearchService : IOperationAdministratorAccountSearchService
{
    private const int SearchKeywordMaxLength = 200;

    private IAccountRepository _accounts { get; }

    public OperationAdministratorAccountSearchService(IAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public async Task<IResult<SearchAccountsResponse>> SearchAccountsAsync(
        SearchAccountsRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<SearchAccountsResponse>();

        var builder = Result.Create<SearchAccountsResponse>();

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

        var query = _accounts.AsQueryable();

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

        var response = new SearchAccountsResponse
        {
            Offset = offset,
            Limit = limit,
            Total = total,
            Items = accounts
                .Select(OperationAdministratorAccountDetailsMapper.ToAccountDetails)
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
