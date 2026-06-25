using Nexus.BankAccounts.Aggregates;

namespace Nexus.BankAccounts.Presentation;

public static class BankAccountApiMapping
{
    public static object ToBankAccountResponse(BankAccount account)
    {
        var totalBrl = account.Balances.Sum(balance => balance.AmountBrl);
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
            balances = account.Balances.Select(ToBankBalance).ToArray(),
            createdAt = account.CreatedAt,
            updatedAt = account.UpdatedAt,
        };
    }

    private static object ToBankBalance(BankBalance balance) => new
    {
        id = balance.Id,
        amountBrl = balance.AmountBrl,
        transferId = balance.TransferId,
        createdAt = balance.CreatedAt,
        splits = balance.Splits,
        appliedStrawManFeeIds = balance.AppliedStrawManFeeIds,
        origin = balance.Origin,
    };
}
