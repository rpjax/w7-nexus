using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.BankAccounts.Aggregates;
using Nexus.BankAccounts.Application.Contracts;
using Nexus.CryptoWallets.Aggregates;
using Nexus.CryptoWallets.Application.Contracts;
using Nexus.Accounts.Application.Contracts;
using Nexus.Payments.Application.Contracts;
using Nexus.Transfers.Aggregates;
using Nexus.Transfers.Application.Contracts;
using Nexus.Transfers.Application.Models;
using Nexus.Transfers.Errors;

namespace Nexus.Transfers.Application.Services;

public sealed class TransferTimelineQueryService : ITransferTimelineQueryService
{
    private readonly ITransferRepository _transfers;
    private readonly IBankAccountRepository _bankAccounts;
    private readonly ICryptoWalletRepository _cryptoWallets;
    private readonly IBankBalanceRepository _bankBalances;
    private readonly ICryptoBalanceRepository _cryptoBalances;
    private readonly IAccountRepository _accounts;
    private readonly IPaymentRepository _payments;

    public TransferTimelineQueryService(
        ITransferRepository transfers,
        IBankAccountRepository bankAccounts,
        ICryptoWalletRepository cryptoWallets,
        IBankBalanceRepository bankBalances,
        ICryptoBalanceRepository cryptoBalances,
        IAccountRepository accounts,
        IPaymentRepository payments)
    {
        _transfers = transfers;
        _bankAccounts = bankAccounts;
        _cryptoWallets = cryptoWallets;
        _bankBalances = bankBalances;
        _cryptoBalances = cryptoBalances;
        _accounts = accounts;
        _payments = payments;
    }

    public async Task<IResult<TransferTimelineDetails>> GetTimelineAsync(string transferId)
    {
        if (string.IsNullOrWhiteSpace(transferId))
        {
            return Result<TransferTimelineDetails>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.TransferIdInvalid)
                .WithMessage("O ID da transferência é obrigatório.")
                .Build());
        }

        var normalizedId = transferId.Trim();
        var focus = await _transfers.AsQueryable()
            .Where(t => t.Id == normalizedId)
            .FirstOrDefaultAsync();
        if (focus is null)
        {
            return Result<TransferTimelineDetails>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.TransferNotFound)
                .WithMessage($"A transferência '{transferId}' não foi encontrada.")
                .Build());
        }

        var strawManAccounts = await _accounts.AsQueryable()
            .Where(a => a.Id == focus.StrawManId)
            .ToArrayAsync();
        var bankAccounts = await _bankAccounts.AsQueryable()
            .Where(a => a.OwnerId == focus.StrawManId)
            .ToArrayAsync();
        var cryptoWallets = await _cryptoWallets.AsQueryable()
            .Where(w => w.OwnerId == focus.StrawManId)
            .ToArrayAsync();
        var strawManTransfers = await _transfers.AsQueryable()
            .Where(t => t.StrawManId == focus.StrawManId)
            .ToArrayAsync();

        var balanceIndex = await BuildBalanceIndexAsync(bankAccounts, cryptoWallets);
        var accountLookup = BuildAccountLookup(strawManAccounts, bankAccounts, cryptoWallets);
        var root = FindRootTransfer(focus, balanceIndex, strawManTransfers);
        var chainIds = BuildChainIds(root, strawManTransfers, balanceIndex);
        var chain = strawManTransfers
            .Where(t => chainIds.Contains(t.Id))
            .OrderBy(t => t.CreatedAt)
            .ToArray();

        (bankAccounts, cryptoWallets, accountLookup) = await IncludeChainDestinationAccountsAsync(
            chain,
            bankAccounts,
            cryptoWallets,
            strawManAccounts,
            accountLookup);
        balanceIndex = await BuildBalanceIndexAsync(bankAccounts, cryptoWallets);

        var paymentIds = chain
            .Where(t => t.Type == TransferType.Withdrawal)
            .SelectMany(t => t.PaymentIds)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var payments = paymentIds.Length == 0
            ? Array.Empty<Nexus.Payments.Aggregates.Payment>()
            : await _payments.AsQueryable()
                .Where(p => paymentIds.Contains(p.Id))
                .ToArrayAsync();
        var paymentLookup = payments.ToDictionary(p => p.Id, StringComparer.Ordinal);

        var activeBalanceIds = new HashSet<string>(StringComparer.Ordinal);
        var activeBalances = await BuildActiveBalancesAsync(
            chainIds,
            bankAccounts,
            cryptoWallets,
            accountLookup,
            activeBalanceIds);

        var strawManSummary = ResolveAccountSummary(focus.StrawManId, accountLookup);
        var steps = chain.Select(transfer =>
        {
            var enriched = EnrichTransfer(transfer, accountLookup, strawManSummary);
            var balanceEffects = BuildBalanceEffects(transfer, balanceIndex, accountLookup);
            var paymentSummaries = transfer.Type == TransferType.Withdrawal
                ? transfer.PaymentIds
                    .Where(paymentLookup.ContainsKey)
                    .Select(id => ToPaymentSummary(paymentLookup[id], accountLookup))
                    .ToArray()
                : Array.Empty<PaymentSummaryDetails>();

            var hasActiveBalance = balanceEffects.Any(e =>
                e.Direction == "Credit"
                && activeBalanceIds.Contains(e.BalanceId));

            return new TransferTimelineStepDetails
            {
                TransferId = transfer.Id,
                Type = transfer.Type,
                CreatedAt = transfer.CreatedAt,
                IsFocus = string.Equals(transfer.Id, focus.Id, StringComparison.Ordinal),
                IsCurrent = hasActiveBalance,
                Title = BuildStepTitle(transfer),
                Summary = BuildStepSummary(transfer, enriched),
                Transfer = enriched,
                BalanceEffects = balanceEffects,
                Payments = paymentSummaries,
            };
        }).ToArray();

        return Result<TransferTimelineDetails>.Success(new TransferTimelineDetails
        {
            RootTransferId = root.Id,
            FocusTransferId = focus.Id,
            StrawMan = strawManSummary,
            Steps = steps,
            ActiveBalances = activeBalances,
        });
    }

    private async Task<(BankAccount[] BankAccounts, CryptoWallet[] CryptoWallets, AccountLookup AccountLookup)>
        IncludeChainDestinationAccountsAsync(
            IReadOnlyList<Transfer> chain,
            BankAccount[] bankAccounts,
            CryptoWallet[] cryptoWallets,
            Nexus.Accounts.Aggregates.Account[] strawManAccounts,
            AccountLookup accountLookup)
    {
        var knownBankIds = bankAccounts.Select(a => a.Id).ToHashSet(StringComparer.Ordinal);
        var knownWalletIds = cryptoWallets.Select(w => w.Id).ToHashSet(StringComparer.Ordinal);
        var extraBankIds = new List<string>();
        var extraWalletIds = new List<string>();

        foreach (var transfer in chain)
        {
            var bankId = transfer.DestinationBankAccount?.BankAccountId?.Trim();
            if (!string.IsNullOrWhiteSpace(bankId) && knownBankIds.Add(bankId))
                extraBankIds.Add(bankId);

            var walletId = transfer.DestinationCryptoWallet?.CryptoWalletId?.Trim();
            if (!string.IsNullOrWhiteSpace(walletId) && knownWalletIds.Add(walletId))
                extraWalletIds.Add(walletId);
        }

        if (extraBankIds.Count == 0 && extraWalletIds.Count == 0)
            return (bankAccounts, cryptoWallets, accountLookup);

        var extraBanks = extraBankIds.Count == 0
            ? Array.Empty<BankAccount>()
            : await _bankAccounts.AsQueryable()
                .Where(a => extraBankIds.Contains(a.Id))
                .ToArrayAsync();
        var extraWallets = extraWalletIds.Count == 0
            ? Array.Empty<CryptoWallet>()
            : await _cryptoWallets.AsQueryable()
                .Where(w => extraWalletIds.Contains(w.Id))
                .ToArrayAsync();

        if (extraBanks.Length == 0 && extraWallets.Length == 0)
            return (bankAccounts, cryptoWallets, accountLookup);

        var mergedBanks = bankAccounts.Concat(extraBanks).ToArray();
        var mergedWallets = cryptoWallets.Concat(extraWallets).ToArray();
        var extraAccountIds = extraBanks.Select(a => a.OwnerId)
            .Concat(extraWallets.Select(w => w.OwnerId))
            .Distinct(StringComparer.Ordinal)
            .Where(id => !accountLookup.Accounts.ContainsKey(id))
            .ToArray();
        var extraAccounts = extraAccountIds.Length == 0
            ? Array.Empty<Nexus.Accounts.Aggregates.Account>()
            : await _accounts.AsQueryable()
                .Where(a => extraAccountIds.Contains(a.Id))
                .ToArrayAsync();
        var mergedAccounts = strawManAccounts.Concat(extraAccounts).ToArray();
        var mergedLookup = BuildAccountLookup(mergedAccounts, mergedBanks, mergedWallets);

        return (mergedBanks, mergedWallets, mergedLookup);
    }

    private static Transfer FindRootTransfer(
        Transfer focus,
        IReadOnlyDictionary<string, BalanceReference> balanceIndex,
        IReadOnlyList<Transfer> strawManTransfers)
    {
        var current = focus;
        var visited = new HashSet<string>(StringComparer.Ordinal);

        while (current.Type != TransferType.Withdrawal)
        {
            if (string.IsNullOrWhiteSpace(current.SourceBalanceId))
                break;

            if (!visited.Add(current.Id))
                break;

            if (!balanceIndex.TryGetValue(current.SourceBalanceId, out var balance))
                break;

            var parent = strawManTransfers.FirstOrDefault(t => t.Id == balance.TransferId);
            if (parent is null)
                break;

            current = parent;
        }

        return current.Type == TransferType.Withdrawal ? current : focus;
    }

    private static HashSet<string> BuildChainIds(
        Transfer root,
        IReadOnlyList<Transfer> strawManTransfers,
        IReadOnlyDictionary<string, BalanceReference> balanceIndex)
    {
        var chainIds = new HashSet<string>(StringComparer.Ordinal) { root.Id };
        var changed = true;

        while (changed)
        {
            changed = false;
            foreach (var transfer in strawManTransfers)
            {
                if (chainIds.Contains(transfer.Id))
                    continue;

                if (string.IsNullOrWhiteSpace(transfer.SourceBalanceId))
                    continue;

                if (!balanceIndex.TryGetValue(transfer.SourceBalanceId, out var balance))
                    continue;

                if (chainIds.Contains(balance.TransferId))
                {
                    chainIds.Add(transfer.Id);
                    changed = true;
                }
            }
        }

        return chainIds;
    }

    private async Task<IReadOnlyList<ActiveBalanceDetails>> BuildActiveBalancesAsync(
        HashSet<string> chainIds,
        IReadOnlyList<BankAccount> bankAccounts,
        IReadOnlyList<CryptoWallet> cryptoWallets,
        AccountLookup accountLookup,
        HashSet<string> activeBalanceIds)
    {
        var balances = new List<ActiveBalanceDetails>();
        var bankAccountIds = bankAccounts.Select(a => a.Id).ToArray();
        var walletIds = cryptoWallets.Select(w => w.Id).ToArray();

        var bankBalanceList = bankAccountIds.Length == 0
            ? Array.Empty<BankBalance>()
            : await _bankBalances.AsQueryable()
                .Where(b => bankAccountIds.Contains(b.BankAccountId))
                .ToArrayAsync();

        var cryptoBalanceList = walletIds.Length == 0
            ? Array.Empty<CryptoBalance>()
            : await _cryptoBalances.AsQueryable()
                .Where(b => walletIds.Contains(b.CryptoWalletId))
                .ToArrayAsync();

        var bankAccountsById = bankAccounts.ToDictionary(a => a.Id, StringComparer.Ordinal);
        var walletsById = cryptoWallets.ToDictionary(w => w.Id, StringComparer.Ordinal);

        foreach (var balance in bankBalanceList.Where(b => b.AmountBrl > 0 && chainIds.Contains(b.TransferId)))
        {
            if (!bankAccountsById.TryGetValue(balance.BankAccountId, out var account))
                continue;

            activeBalanceIds.Add(balance.Id);
            balances.Add(new ActiveBalanceDetails
            {
                BalanceId = balance.Id,
                TransferId = balance.TransferId,
                Amount = balance.AmountBrl,
                Chain = null,
                Asset = null,
                Currency = "BRL",
                Account = EnrichBankAccount(account),
                CanMove = true,
                CanPayout = true,
            });
        }

        foreach (var balance in cryptoBalanceList.Where(b => b.Amount > 0 && chainIds.Contains(b.TransferId)))
        {
            if (!walletsById.TryGetValue(balance.CryptoWalletId, out var wallet))
                continue;

            activeBalanceIds.Add(balance.Id);
            balances.Add(new ActiveBalanceDetails
            {
                BalanceId = balance.Id,
                TransferId = balance.TransferId,
                Amount = balance.Amount,
                Chain = balance.Chain.ToString(),
                Asset = balance.Asset.ToString(),
                Currency = balance.Asset.ToString(),
                Account = EnrichCryptoWallet(wallet),
                CanMove = true,
                CanPayout = false,
            });
        }

        return balances
            .OrderByDescending(b => b.Amount)
            .ThenBy(b => b.Account.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<BalanceEffectDetails> BuildBalanceEffects(
        Transfer transfer,
        IReadOnlyDictionary<string, BalanceReference> balanceIndex,
        AccountLookup accountLookup)
    {
        var effects = new List<BalanceEffectDetails>();

        foreach (var balance in balanceIndex.Values.Where(b => b.TransferId == transfer.Id))
        {
            effects.Add(new BalanceEffectDetails
            {
                Direction = "Credit",
                BalanceId = balance.BalanceId,
                Amount = balance.Amount,
                Chain = balance.Chain,
                Asset = balance.Asset,
                Currency = balance.Asset ?? "BRL",
                Account = balance.Kind == "BankAccount"
                    ? EnrichBankAccount(balance.BankAccount!)
                    : EnrichCryptoWallet(balance.CryptoWallet!),
            });
        }

        if (!string.IsNullOrWhiteSpace(transfer.SourceBalanceId)
            && balanceIndex.TryGetValue(transfer.SourceBalanceId, out var sourceBalance))
        {
            effects.Add(new BalanceEffectDetails
            {
                Direction = "Debit",
                BalanceId = sourceBalance.BalanceId,
                Amount = transfer.SourceAmount,
                Chain = sourceBalance.Chain,
                Asset = sourceBalance.Asset,
                Currency = sourceBalance.Asset ?? "BRL",
                Account = sourceBalance.Kind == "BankAccount"
                    ? EnrichBankAccount(sourceBalance.BankAccount!)
                    : EnrichCryptoWallet(sourceBalance.CryptoWallet!),
            });
        }

        return effects;
    }

    private TransferEnrichedDetails EnrichTransfer(
        Transfer transfer,
        AccountLookup accountLookup,
        AccountSummaryDetails? strawManSummary)
    {
        strawManSummary ??= ResolveAccountSummary(transfer.StrawManId, accountLookup)
            ?? new AccountSummaryDetails { Id = transfer.StrawManId, Username = transfer.StrawManId };

        return new TransferEnrichedDetails
        {
            Id = transfer.Id,
            Type = transfer.Type,
            OnrampingMethod = transfer.OnrampingMethod?.ToString(),
            Proof = transfer.Proof is null
                ? null
                : new TransferProofDetails
                {
                    PixTransactionId = transfer.Proof.PixTransactionId,
                    PixAuthenticationCode = transfer.Proof.PixAuthenticationCode,
                    CryptoTransactionId = transfer.Proof.CryptoTransactionId,
                },
            Source = EnrichOrigin(transfer, accountLookup),
            Destination = EnrichDestination(transfer, accountLookup),
            SourceAmount = transfer.SourceAmount,
            ProducedAmount = transfer.ProducedAmount,
            ProducedAsset = transfer.ProducedAsset?.ToString(),
            ProducedChain = transfer.ProducedChain?.ToString(),
            PaymentIds = transfer.PaymentIds,
            SourceBalanceId = transfer.SourceBalanceId,
            StrawMan = strawManSummary,
            CreatedAt = transfer.CreatedAt,
        };
    }

    private static TransferEndpointDetails? EnrichOrigin(Transfer transfer, AccountLookup accountLookup)
    {
        if (transfer.OriginType is null)
            return null;

        if (transfer.OriginType == TransferOriginType.BankAccount
            && transfer.OriginBankAccount is not null
            && accountLookup.BankAccounts.TryGetValue(transfer.OriginBankAccount.BankAccountId, out var bank))
            return EnrichBankAccount(bank);

        if (transfer.OriginType == TransferOriginType.CryptoWallet
            && transfer.OriginCryptoWallet is not null
            && accountLookup.CryptoWallets.TryGetValue(transfer.OriginCryptoWallet.CryptoWalletId, out var wallet))
            return EnrichCryptoWallet(wallet);

        return new TransferEndpointDetails
        {
            Kind = transfer.OriginType.ToString()!,
            Id = transfer.OriginBankAccount?.BankAccountId ?? transfer.OriginCryptoWallet?.CryptoWalletId,
            DisplayName = transfer.OriginType.ToString()!,
        };
    }

    private static TransferEndpointDetails? EnrichDestination(Transfer transfer, AccountLookup accountLookup)
    {
        if (transfer.DestinationType is null)
            return null;

        if (transfer.DestinationType == TransferDestinationType.BankAccount
            && transfer.DestinationBankAccount is not null
            && accountLookup.BankAccounts.TryGetValue(transfer.DestinationBankAccount.BankAccountId, out var bank))
            return EnrichBankAccount(bank);

        if (transfer.DestinationType == TransferDestinationType.CryptoWallet
            && transfer.DestinationCryptoWallet is not null
            && accountLookup.CryptoWallets.TryGetValue(transfer.DestinationCryptoWallet.CryptoWalletId, out var wallet))
            return EnrichCryptoWallet(wallet);

        return new TransferEndpointDetails
        {
            Kind = transfer.DestinationType.ToString()!,
            Id = transfer.DestinationBankAccount?.BankAccountId ?? transfer.DestinationCryptoWallet?.CryptoWalletId,
            DisplayName = transfer.DestinationType.ToString()!,
        };
    }

    private static TransferEndpointDetails EnrichBankAccount(BankAccount account)
    {
        var label = string.IsNullOrWhiteSpace(account.Label) ? null : account.Label.Trim();
        var bankSummary = $"{account.Bank} · Ag {account.Agency} · Cc {account.AccountNumber}{account.AccountDigit}";
        var displayName = string.IsNullOrWhiteSpace(label) ? bankSummary : label;

        return new TransferEndpointDetails
        {
            Kind = "BankAccount",
            Id = account.Id,
            DisplayName = displayName,
            Label = label,
            BankSummary = bankSummary,
        };
    }

    private static TransferEndpointDetails EnrichCryptoWallet(CryptoWallet wallet)
    {
        var label = string.IsNullOrWhiteSpace(wallet.Label) ? null : wallet.Label.Trim();
        var addressParts = wallet.Addresses
            .Select(a => $"{a.Namespace}: {Shorten(a.Address, 6, 4)}")
            .ToArray();
        var cryptoSummary = addressParts.Length > 0
            ? string.Join(" · ", addressParts)
            : "Sem endereços";
        var displayName = string.IsNullOrWhiteSpace(label) ? cryptoSummary : label;

        return new TransferEndpointDetails
        {
            Kind = "CryptoWallet",
            Id = wallet.Id,
            DisplayName = displayName,
            Label = label,
            CryptoSummary = cryptoSummary,
        };
    }

    private static TransferEndpointDetails EnrichStrawMan(string strawManId, AccountLookup accountLookup)
    {
        var summary = ResolveAccountSummary(strawManId, accountLookup);
        var username = summary?.Username ?? strawManId;

        return new TransferEndpointDetails
        {
            Kind = "StrawMan",
            Id = strawManId,
            DisplayName = username,
            Username = username,
        };
    }

    private static AccountSummaryDetails? ResolveAccountSummary(string accountId, AccountLookup accountLookup)
    {
        if (!accountLookup.Accounts.TryGetValue(accountId, out var account))
            return null;

        return new AccountSummaryDetails
        {
            Id = account.Id,
            Username = account.Username,
        };
    }

    private static PaymentSummaryDetails ToPaymentSummary(
        Nexus.Payments.Aggregates.Payment payment,
        AccountLookup accountLookup)
    {
        string? operatorUsername = null;
        if (!string.IsNullOrWhiteSpace(payment.OperatorId)
            && accountLookup.Accounts.TryGetValue(payment.OperatorId, out var operatorAccount))
        {
            operatorUsername = operatorAccount.Username;
        }

        return new PaymentSummaryDetails
        {
            Id = payment.Id,
            Amount = payment.Amount,
            Status = payment.Status.ToString(),
            SettlementStatus = payment.SettlementStatus.ToString(),
            Gateway = payment.Gateway.ToString(),
            GatewayTransactionId = payment.GatewayTransactionId,
            OperatorUsername = operatorUsername,
            CreatedAt = payment.CreatedAt,
        };
    }

    private static string BuildStepTitle(Transfer transfer) =>
        transfer.Type switch
        {
            TransferType.Withdrawal => "Saque",
            TransferType.Movement => "Movimentação",
            TransferType.Payout => "Repasse",
            _ => transfer.Type.ToString(),
        };

    private static string BuildStepSummary(Transfer transfer, TransferEnrichedDetails enriched)
    {
        var amount = FormatAmount(transfer.SourceAmount, transfer.ProducedAsset?.ToString());
        var destination = enriched.Destination?.DisplayName ?? "destino";

        return transfer.Type switch
        {
            TransferType.Withdrawal => $"{amount} creditados em {destination}",
            TransferType.Movement => $"{amount} de {enriched.Source?.DisplayName ?? "origem"} para {destination}",
            TransferType.Payout => $"{amount} repassados para {destination}",
            _ => amount,
        };
    }

    private static string FormatAmount(decimal amount, string? asset)
        => asset is null ? $"R$ {amount:N2}" : $"{amount} {asset}";

    private static string Shorten(string value, int prefix, int suffix)
    {
        if (value.Length <= prefix + suffix + 3)
            return value;

        return $"{value[..prefix]}…{value[^suffix..]}";
    }

    private static AccountLookup BuildAccountLookup(
        IReadOnlyList<Nexus.Accounts.Aggregates.Account> accounts,
        IReadOnlyList<BankAccount> bankAccounts,
        IReadOnlyList<CryptoWallet> cryptoWallets) =>
        new(
            accounts.ToDictionary(a => a.Id, StringComparer.Ordinal),
            bankAccounts.ToDictionary(a => a.Id, StringComparer.Ordinal),
            cryptoWallets.ToDictionary(w => w.Id, StringComparer.Ordinal));

    private async Task<Dictionary<string, BalanceReference>> BuildBalanceIndexAsync(
        IReadOnlyList<BankAccount> bankAccounts,
        IReadOnlyList<CryptoWallet> cryptoWallets)
    {
        var index = new Dictionary<string, BalanceReference>(StringComparer.Ordinal);
        var bankAccountIds = bankAccounts.Select(a => a.Id).ToArray();
        var walletIds = cryptoWallets.Select(w => w.Id).ToArray();

        var bankBalanceList = bankAccountIds.Length == 0
            ? Array.Empty<BankBalance>()
            : await _bankBalances.AsQueryable()
                .Where(b => bankAccountIds.Contains(b.BankAccountId))
                .ToArrayAsync();

        var cryptoBalanceList = walletIds.Length == 0
            ? Array.Empty<CryptoBalance>()
            : await _cryptoBalances.AsQueryable()
                .Where(b => walletIds.Contains(b.CryptoWalletId))
                .ToArrayAsync();

        var bankAccountsById = bankAccounts.ToDictionary(a => a.Id, StringComparer.Ordinal);
        var walletsById = cryptoWallets.ToDictionary(w => w.Id, StringComparer.Ordinal);

        foreach (var balance in bankBalanceList)
        {
            if (!bankAccountsById.TryGetValue(balance.BankAccountId, out var account))
                continue;

            index[balance.Id] = new BalanceReference(
                balance.Id,
                balance.TransferId,
                "BankAccount",
                account.Id,
                balance.AmountBrl,
                null,
                null,
                account,
                null);
        }

        foreach (var balance in cryptoBalanceList)
        {
            if (!walletsById.TryGetValue(balance.CryptoWalletId, out var wallet))
                continue;

            index[balance.Id] = new BalanceReference(
                balance.Id,
                balance.TransferId,
                "CryptoWallet",
                wallet.Id,
                balance.Amount,
                balance.Chain.ToString(),
                balance.Asset.ToString(),
                null,
                wallet);
        }

        return index;
    }

    private sealed record AccountLookup(
        IReadOnlyDictionary<string, Nexus.Accounts.Aggregates.Account> Accounts,
        IReadOnlyDictionary<string, BankAccount> BankAccounts,
        IReadOnlyDictionary<string, CryptoWallet> CryptoWallets);

    private sealed record BalanceReference(
        string BalanceId,
        string TransferId,
        string Kind,
        string AccountId,
        decimal Amount,
        string? Chain,
        string? Asset,
        BankAccount? BankAccount,
        CryptoWallet? CryptoWallet);
}
