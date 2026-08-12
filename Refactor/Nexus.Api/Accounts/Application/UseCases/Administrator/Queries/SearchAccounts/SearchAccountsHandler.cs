using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Authorization.Administrator;
using Refactor.Nexus.Api.Accounts.Application.DTOs;
using Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Queries;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Domain.Errors;

namespace Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Queries.SearchAccounts;

public sealed record SearchAccountsQuery(
    int Limit,
    int Offset,
    string? Keyword,
    string? Status = null,
    string? Role = null);

public sealed class SearchAccountsResult
{
    public required int Offset { get; init; }
    public required int Limit { get; init; }
    public required int Total { get; init; }
    public IReadOnlyList<AccountDetailsView> Items { get; init; } = Array.Empty<AccountDetailsView>();
}

public sealed class SearchAccountsHandler : ISearchAccountsUseCase
{
    private const int SearchKeywordMaxLength = 200;

    private readonly IRequestContext _requestContext;
    private readonly IAdministratorAccessPolicy _accessPolicy;
    private readonly IAccountReadRepository _accountReadRepository;

    public SearchAccountsHandler(
        IRequestContext requestContext,
        IAdministratorAccessPolicy accessPolicy,
        IAccountReadRepository accountReadRepository)
    {
        _requestContext = requestContext;
        _accessPolicy = accessPolicy;
        _accountReadRepository = accountReadRepository;
    }

    public async Task<IOperationResult<SearchAccountsResult>> HandleAsync(
        SearchAccountsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query is null)
            return OperationResult<SearchAccountsResult>.Failure(RequestBodyRequiredError());

        var requesterResult = await _requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return OperationResult<SearchAccountsResult>.Failure(requesterResult.Errors);

        var authorization = await _accessPolicy.AuthorizeAsync(requester.Roles, cancellationToken);
        if (authorization.IsFailure)
            return OperationResult<SearchAccountsResult>.Failure(authorization.Errors);
        if (!authorization.IsAuthorized)
            return OperationResult<SearchAccountsResult>.Unauthorized(authorization.AuthorizationErrors);

        var errors = Validate(query);
        if (errors.Count > 0)
            return OperationResult<SearchAccountsResult>.Failure(errors);

        var limit = query.Limit <= 0 ? 20 : query.Limit;
        var keyword = query.Keyword?.Trim();
        var status = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim();
        var role = string.IsNullOrWhiteSpace(query.Role) ? null : query.Role.Trim();
        var (accounts, total) = await _accountReadRepository.SearchAsync(
            keyword,
            status,
            role,
            query.Offset,
            limit,
            cancellationToken);

        return OperationResult<SearchAccountsResult>.Success(new SearchAccountsResult
        {
            Offset = query.Offset,
            Limit = limit,
            Total = total,
            Items = accounts.Select(AccountDetailsView.FromAccount).ToArray()
        });
    }

    private static List<Error> Validate(SearchAccountsQuery query)
    {
        var errors = new List<Error>();
        var limit = query.Limit <= 0 ? 20 : query.Limit;

        if (limit is < 1 or >= 1000)
            errors.Add(BuildError(AccountErrorCodes.SearchLimitInvalid, "O limite deve estar entre 1 e 999."));

        if (query.Offset < 0)
            errors.Add(BuildError(AccountErrorCodes.SearchOffsetInvalid, "O deslocamento nao pode ser negativo."));

        if (!string.IsNullOrWhiteSpace(query.Keyword) && query.Keyword.Trim().Length > SearchKeywordMaxLength)
            errors.Add(BuildError(AccountErrorCodes.SearchKeywordTooLong, $"A palavra-chave pode ter no maximo {SearchKeywordMaxLength} caracteres."));

        return errors;
    }

    private static Error BuildError(string code, string message) =>
        Error.Create()
            .WithCode(code)
            .WithMessage(message)
            .Build();

    private static Error RequestBodyRequiredError() =>
        BuildError(AccountErrorCodes.RequestBodyRequired, "O corpo da requisicao e obrigatorio.");
}
