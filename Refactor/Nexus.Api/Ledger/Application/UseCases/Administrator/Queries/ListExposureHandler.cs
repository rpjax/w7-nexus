using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Shared;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount;

namespace Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Queries;

public sealed record ExposureLine(Guid AccountId, string Currency, decimal Amount, string BalanceStatus);
public sealed record ListExposureQuery;
public sealed record ListExposureResult(IReadOnlyList<ExposureLine> Items);

public interface IListExposureUseCase
{
    Task<IOperationResult<ListExposureResult>> HandleAsync(
        ListExposureQuery query,
        CancellationToken cancellationToken = default);
}

public sealed class ListExposureHandler : IListExposureUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly ILedgerAccess _access;
    private readonly IWorldAccountRepository _accounts;
    private readonly IClaimRepository _claims;

    public ListExposureHandler(
        IRequestContext requestContext,
        ILedgerAccess access,
        IWorldAccountRepository accounts,
        IClaimRepository claims)
    {
        _requestContext = requestContext;
        _access = access;
        _accounts = accounts;
        _claims = claims;
    }

    public async Task<IOperationResult<ListExposureResult>> HandleAsync(
        ListExposureQuery query,
        CancellationToken cancellationToken = default)
    {
        var auth = await LedgerGuards.AuthorizeAsync<ListExposureResult>(_requestContext, _access, cancellationToken);
        if (auth.Failure is not null)
            return auth.Failure;

        var accounts = (await _accounts.ListAsync(cancellationToken))
            .Where(a => a.BalanceStatus is BalanceStatus.Frozen or BalanceStatus.Lost)
            .ToList();
        var lines = new List<ExposureLine>();
        foreach (var account in accounts)
        {
            var claims = await _claims.ListAsync(null, account.Id, null, cancellationToken);
            foreach (var group in claims.Where(c => c.IsActive).GroupBy(c => c.Currency, StringComparer.OrdinalIgnoreCase))
            {
                lines.Add(new ExposureLine(account.Id, group.Key, group.Sum(c => c.Amount), account.BalanceStatus.ToString()));
            }
        }

        return OperationResult<ListExposureResult>.Success(new ListExposureResult(lines));
    }
}
