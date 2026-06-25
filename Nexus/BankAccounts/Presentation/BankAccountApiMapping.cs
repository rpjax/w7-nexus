using Nexus.BankAccounts.Aggregates;

namespace Nexus.BankAccounts.Presentation;

public static class BankAccountApiMapping
{
    public static object ToBankAccountResponse(BankAccount account, IReadOnlyList<BankBalance> balances)
    {
        var totalBrl = balances.Sum(balance => balance.AmountBrl);
        return new
        {
            id = account.Id,
            ownerId = account.OwnerId,
            bank = account.Bank.ToString(),
            agency = account.Agency,
            accountNumber = account.AccountNumber,
            accountDigit = account.AccountDigit,
            accountType = account.AccountType.ToString(),
            label = account.Label,
            totalBalanceBrl = totalBrl,
            balances = balances.Select(ToBankBalance).ToArray(),
            createdAt = account.CreatedAt,
            updatedAt = account.UpdatedAt,
        };
    }

    private static object ToBankBalance(BankBalance balance) => new
    {
        id = balance.Id,
        bankAccountId = balance.BankAccountId,
        amountBrl = balance.AmountBrl,
        transferId = balance.TransferId,
        createdAt = balance.CreatedAt,
        splits = balance.Splits,
        origin = ToBalanceOrigin(balance.Origin),
    };

    private static object ToBalanceOrigin(BankBalanceOrigin origin) => new
    {
        operationId = origin.OperationId,
        operatorId = origin.OperatorId,
    };
}
