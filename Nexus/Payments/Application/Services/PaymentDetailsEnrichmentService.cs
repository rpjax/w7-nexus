using Aidan.Core.Linq.Extensions;
using Nexus.Accounts.Aggregates;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;
using Nexus.Operations.Application.Contracts;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Models;

namespace Nexus.Payments.Application.Services;

public sealed class PaymentDetailsEnrichmentService : IPaymentDetailsEnrichmentService
{
    private readonly IAccountRepository _accounts;
    private readonly IOperationRepository _operations;

    public PaymentDetailsEnrichmentService(
        IAccountRepository accounts,
        IOperationRepository operations)
    {
        _accounts = accounts;
        _operations = operations;
    }

    public async Task<PaymentDetails> EnrichAsync(
        PaymentDetails details,
        CancellationToken cancellationToken = default)
    {
        var enriched = await EnrichManyAsync([details], cancellationToken);
        return enriched[0];
    }

    public async Task<IReadOnlyList<PaymentDetails>> EnrichManyAsync(
        IReadOnlyList<PaymentDetails> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
            return items;

        var operationIds = items
            .Select(item => item.OperationId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var accountIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.OperatorId))
                accountIds.Add(item.OperatorId);
            if (!string.IsNullOrWhiteSpace(item.StrawManId))
                accountIds.Add(item.StrawManId);
            foreach (var split in item.Splits)
            {
                if (!string.IsNullOrWhiteSpace(split.AccountId))
                    accountIds.Add(split.AccountId);
            }
        }

        var operations = operationIds.Length == 0
            ? Array.Empty<Operations.Aggregates.Operation>()
            : await _operations.AsQueryable()
                .Where(operation => operationIds.Contains(operation.Id))
                .ToArrayAsync();

        var accounts = accountIds.Count == 0
            ? Array.Empty<Account>()
            : await _accounts.AsQueryable()
                .Where(account => accountIds.Contains(account.Id))
                .ToArrayAsync();

        var operationLookup = operations.ToDictionary(operation => operation.Id, StringComparer.Ordinal);
        var accountLookup = accounts.ToDictionary(account => account.Id, StringComparer.Ordinal);

        return items
            .Select(item => EnrichItem(item, operationLookup, accountLookup))
            .ToArray();
    }

    private static PaymentDetails EnrichItem(
        PaymentDetails item,
        IReadOnlyDictionary<string, Operations.Aggregates.Operation> operationLookup,
        IReadOnlyDictionary<string, Account> accountLookup)
    {
        operationLookup.TryGetValue(item.OperationId, out var operation);
        Account? operatorAccount = null;
        if (!string.IsNullOrWhiteSpace(item.OperatorId))
            accountLookup.TryGetValue(item.OperatorId, out operatorAccount);
        accountLookup.TryGetValue(item.StrawManId, out var strawManAccount);

        return new PaymentDetails
        {
            Id = item.Id,
            OperationId = item.OperationId,
            OperationName = operation?.Name,
            OperatorId = item.OperatorId,
            OperatorUsername = operatorAccount?.Username,
            StrawManId = item.StrawManId,
            StrawManUsername = strawManAccount?.Username,
            Gateway = item.Gateway,
            GatewayTransactionId = item.GatewayTransactionId,
            Amount = item.Amount,
            Splits = item.Splits
                .Select(split => EnrichSplit(split, accountLookup))
                .ToArray(),
            Status = item.Status,
            SettlementStatus = item.SettlementStatus,
            DistributionStatus = item.DistributionStatus,
            CreatedAt = item.CreatedAt,
            PaidAt = item.PaidAt,
            RefundedAt = item.RefundedAt,
            KilledAt = item.KilledAt,
            KillReason = item.KillReason,
            WithdrawnAt = item.WithdrawnAt,
            DistributedAt = item.DistributedAt,
        };
    }

    private static PaymentSplitDetails EnrichSplit(
        PaymentSplitDetails split,
        IReadOnlyDictionary<string, Account> accountLookup)
    {
        accountLookup.TryGetValue(split.AccountId, out var account);

        return new PaymentSplitDetails
        {
            AccountId = split.AccountId,
            Username = account?.Username,
            Role = account is null ? null : ResolvePrimaryRole(account),
            Percentage = split.Percentage,
            Amount = split.Amount,
        };
    }

    private static string ResolvePrimaryRole(Account account)
    {
        if (account.Roles.Contains(Roles.StrawMan, StringComparer.Ordinal))
            return Roles.StrawMan;
        if (account.Roles.Contains(Roles.Operator, StringComparer.Ordinal))
            return Roles.Operator;
        if (account.Roles.Contains(Roles.OlxOperator, StringComparer.Ordinal))
            return Roles.OlxOperator;
        if (account.Roles.Contains(Roles.Administrator, StringComparer.Ordinal))
            return Roles.Administrator;
        return account.Roles.FirstOrDefault() ?? "Account";
    }
}
