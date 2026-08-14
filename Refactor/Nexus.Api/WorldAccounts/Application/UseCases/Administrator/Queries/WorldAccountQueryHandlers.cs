using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Application.UseCases.Shared;

namespace Refactor.Nexus.Api.WorldAccounts.Application.UseCases.Administrator.Queries;

public sealed record WorldAccountView(
    Guid AccountId,
    string Kind,
    string Label,
    Guid? OrangeMemberId,
    decimal? Level1CutPercent,
    string EmissionStatus,
    string BalanceStatus,
    IReadOnlyDictionary<string, decimal> Balances,
    IReadOnlyDictionary<string, decimal> Quotas,
    DateTime CreatedAt,
    DateTime LastUpdatedAt);

public sealed record ListWorldAccountsResult(IReadOnlyList<WorldAccountView> Items);
public interface IListWorldAccountsUseCase
{
    Task<IOperationResult<ListWorldAccountsResult>> HandleAsync(CancellationToken cancellationToken = default);
}

public sealed class ListWorldAccountsHandler : IListWorldAccountsUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IWorldAccountAccess _access;
    private readonly IWorldAccountRepository _repository;

    public ListWorldAccountsHandler(
        IRequestContext requestContext,
        IWorldAccountAccess access,
        IWorldAccountRepository repository)
    {
        _requestContext = requestContext;
        _access = access;
        _repository = repository;
    }

    public async Task<IOperationResult<ListWorldAccountsResult>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var auth = await WorldAccountGuards.AuthorizeManageAsync<ListWorldAccountsResult>(
            _requestContext, _access, cancellationToken);
        if (auth.Failure is not null)
            return auth.Failure;

        var items = await _repository.ListAsync(cancellationToken);
        return OperationResult<ListWorldAccountsResult>.Success(
            new ListWorldAccountsResult(items.Select(WorldAccountViews.ToView).ToList()));
    }
}

public sealed record GetWorldAccountQuery(string AccountId);
public interface IGetWorldAccountUseCase
{
    Task<IOperationResult<WorldAccountView>> HandleAsync(GetWorldAccountQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetWorldAccountHandler : IGetWorldAccountUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IWorldAccountAccess _access;
    private readonly IWorldAccountRepository _repository;

    public GetWorldAccountHandler(
        IRequestContext requestContext,
        IWorldAccountAccess access,
        IWorldAccountRepository repository)
    {
        _requestContext = requestContext;
        _access = access;
        _repository = repository;
    }

    public async Task<IOperationResult<WorldAccountView>> HandleAsync(
        GetWorldAccountQuery query,
        CancellationToken cancellationToken = default)
    {
        var auth = await WorldAccountGuards.AuthorizeManageAsync<WorldAccountView>(
            _requestContext, _access, cancellationToken);
        if (auth.Failure is not null)
            return auth.Failure;

        if (!Guid.TryParse(query.AccountId, out var id))
            return OperationResult<WorldAccountView>.Failure(WorldAccountGuards.NotFound(query.AccountId));

        var account = await _repository.GetByIdAsync(id, cancellationToken);
        if (account is null)
            return OperationResult<WorldAccountView>.Failure(WorldAccountGuards.NotFound(query.AccountId));

        return OperationResult<WorldAccountView>.Success(WorldAccountViews.ToView(account));
    }
}

public sealed record ListWorldAccountTransactionsQuery(string AccountId);
public sealed record ListWorldAccountTransactionsResult(IReadOnlyList<WorldAccountTransaction> Items);
public interface IListWorldAccountTransactionsUseCase
{
    Task<IOperationResult<ListWorldAccountTransactionsResult>> HandleAsync(
        ListWorldAccountTransactionsQuery query,
        CancellationToken cancellationToken = default);
}

public sealed class ListWorldAccountTransactionsHandler : IListWorldAccountTransactionsUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IWorldAccountAccess _access;
    private readonly IWorldAccountRepository _repository;

    public ListWorldAccountTransactionsHandler(
        IRequestContext requestContext,
        IWorldAccountAccess access,
        IWorldAccountRepository repository)
    {
        _requestContext = requestContext;
        _access = access;
        _repository = repository;
    }

    public async Task<IOperationResult<ListWorldAccountTransactionsResult>> HandleAsync(
        ListWorldAccountTransactionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var auth = await WorldAccountGuards.AuthorizeManageAsync<ListWorldAccountTransactionsResult>(
            _requestContext, _access, cancellationToken);
        if (auth.Failure is not null)
            return auth.Failure;

        if (!Guid.TryParse(query.AccountId, out var id))
            return OperationResult<ListWorldAccountTransactionsResult>.Failure(WorldAccountGuards.NotFound(query.AccountId));

        var items = await _repository.ListTransactionsAsync(id, cancellationToken);
        return OperationResult<ListWorldAccountTransactionsResult>.Success(new ListWorldAccountTransactionsResult(items));
    }
}

internal static class WorldAccountViews
{
    public static WorldAccountView ToView(Domain.Aggregates.WorldAccount.WorldAccount account) =>
        new(
            account.Id,
            account.Kind.ToString(),
            account.Label,
            account.OrangeMemberId,
            account.Level1CutPercent,
            account.EmissionStatus.ToString(),
            account.BalanceStatus.ToString(),
            account.Balances.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase),
            account.Quotas.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase),
            account.CreatedAt,
            account.LastUpdatedAt);
}
