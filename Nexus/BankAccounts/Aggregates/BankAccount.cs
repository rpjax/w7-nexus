using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.BankAccounts.Errors;

namespace Nexus.BankAccounts.Aggregates;

public enum BankAccountType
{
    Checking = 0,
    Savings,
}

public sealed class BankAccount
{
    public const int MaxAgencyLength = 10;
    public const int MaxAccountNumberLength = 20;
    public const int MaxAccountDigitLength = 2;
    public const int MaxLabelLength = 100;

    public string Id { get; }
    public string StrawManId { get; }
    public BrazilianBank Bank { get; }
    public string Agency { get; private set; }
    public string AccountNumber { get; private set; }
    public string? AccountDigit { get; private set; }
    public BankAccountType AccountType { get; private set; }
    public string? Label { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<BankBalance> _balances;
    public IReadOnlyList<BankBalance> Balances => _balances;

    internal BankAccount(
        string id,
        string strawManId,
        BrazilianBank bank,
        string agency,
        string accountNumber,
        string? accountDigit,
        BankAccountType accountType,
        string? label,
        DateTime createdAt,
        DateTime updatedAt,
        IReadOnlyList<BankBalance>? balances = null)
    {
        Id = id;
        StrawManId = strawManId;
        Bank = bank;
        Agency = agency;
        AccountNumber = accountNumber;
        AccountDigit = accountDigit;
        AccountType = accountType;
        Label = label;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        _balances = balances?.ToList() ?? new List<BankBalance>();
    }

    public static IResult<BankAccount> Create(
        string ownerId,
        BrazilianBank bank,
        string agency,
        string accountNumber,
        string? accountDigit,
        BankAccountType accountType,
        string? label)
    {
        var builder = Result.Create<BankAccount>();

        ownerId = ownerId?.Trim() ?? string.Empty;
        agency = agency?.Trim() ?? string.Empty;
        accountNumber = accountNumber?.Trim() ?? string.Empty;
        accountDigit = string.IsNullOrWhiteSpace(accountDigit) ? null : accountDigit.Trim();
        label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();

        if (string.IsNullOrWhiteSpace(ownerId))
            builder.WithError(Error.Create()
                .WithCode(BankAccountErrorCodes.OwnerInvalid)
                .WithMessage("O ID do dono da conta é obrigatório.")
                .Build());

        if (!Enum.IsDefined(bank))
            builder.WithError(Error.Create()
                .WithCode(BankAccountErrorCodes.BankInvalid)
                .WithMessage("O banco informado é inválido.")
                .Build());

        if (string.IsNullOrWhiteSpace(agency))
            builder.WithError(Error.Create()
                .WithCode(BankAccountErrorCodes.AgencyInvalid)
                .WithMessage("A agência é obrigatória.")
                .Build());
        else if (agency.Length > MaxAgencyLength)
            builder.WithError(Error.Create()
                .WithCode(BankAccountErrorCodes.AgencyTooLong)
                .WithMessage($"A agência pode ter no máximo {MaxAgencyLength} caracteres.")
                .Build());

        if (string.IsNullOrWhiteSpace(accountNumber))
            builder.WithError(Error.Create()
                .WithCode(BankAccountErrorCodes.AccountNumberInvalid)
                .WithMessage("O número da conta é obrigatório.")
                .Build());
        else if (accountNumber.Length > MaxAccountNumberLength)
            builder.WithError(Error.Create()
                .WithCode(BankAccountErrorCodes.AccountNumberTooLong)
                .WithMessage($"O número da conta pode ter no máximo {MaxAccountNumberLength} caracteres.")
                .Build());

        if (accountDigit is not null && accountDigit.Length > MaxAccountDigitLength)
            builder.WithError(Error.Create()
                .WithCode(BankAccountErrorCodes.AccountDigitTooLong)
                .WithMessage($"O dígito da conta pode ter no máximo {MaxAccountDigitLength} caracteres.")
                .Build());

        if (!Enum.IsDefined(accountType))
            builder.WithError(Error.Create()
                .WithCode(BankAccountErrorCodes.AccountTypeInvalid)
                .WithMessage("O tipo de conta informado é inválido.")
                .Build());

        if (label is not null && label.Length > MaxLabelLength)
            builder.WithError(Error.Create()
                .WithCode(BankAccountErrorCodes.LabelTooLong)
                .WithMessage($"O rótulo pode ter no máximo {MaxLabelLength} caracteres.")
                .Build());

        if (builder.ContainsError)
            return builder.Build();

        var now = DateTime.UtcNow;
        return builder.WithValue(new BankAccount(
            id: string.Empty,
            strawManId: ownerId,
            bank: bank,
            agency: agency,
            accountNumber: accountNumber,
            accountDigit: accountDigit,
            accountType: accountType,
            label: label,
            createdAt: now,
            updatedAt: now)).Build();
    }

    public IResult UpdateLabel(string? label)
    {
        label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();

        if (label is not null && label.Length > MaxLabelLength)
            return Result.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.LabelTooLong)
                .WithMessage($"O rótulo pode ter no máximo {MaxLabelLength} caracteres.")
                .Build());

        Label = label;
        Touch();
        return Result.Success();
    }

    public IResult CreditBalance(BankBalance balance)
    {
        ArgumentNullException.ThrowIfNull(balance);
        _balances.Add(balance);
        Touch();
        return Result.Success();
    }

    public IResult<BankDebitPartialResult> DebitPartialBalance(string balanceId, decimal amountBrl)
    {
        balanceId = balanceId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(balanceId))
            return Result<BankDebitPartialResult>.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.BalanceIdInvalid)
                .WithMessage("O ID do saldo é obrigatório.")
                .Build());

        if (amountBrl <= 0)
            return Result<BankDebitPartialResult>.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.BalanceAmountInvalid)
                .WithMessage("O valor do débito deve ser maior que zero.")
                .Build());

        var index = _balances.FindIndex(b => b.Id == balanceId);
        if (index < 0)
            return Result<BankDebitPartialResult>.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.BalanceNotFound)
                .WithMessage($"O saldo '{balanceId}' não foi encontrado.")
                .Build());

        var balance = _balances[index];

        if (amountBrl > balance.AmountBrl)
            return Result<BankDebitPartialResult>.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.BalanceInsufficient)
                .WithMessage("O saldo é insuficiente para o débito solicitado.")
                .Build());

        BankBalance debitedBalance;
        BankBalance? remainderBalance = null;

        if (amountBrl == balance.AmountBrl)
        {
            _balances.RemoveAt(index);
            debitedBalance = balance;
        }
        else
        {
            var remainderAmount = balance.AmountBrl - amountBrl;
            remainderBalance = balance.WithAmount(remainderAmount);
            debitedBalance = balance.WithId(Guid.NewGuid().ToString("N")).WithAmount(amountBrl);
            _balances[index] = remainderBalance;
        }

        Touch();
        return Result<BankDebitPartialResult>.Success(new BankDebitPartialResult(debitedBalance, remainderBalance));
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
