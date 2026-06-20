using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Withdrawals.Errors;

namespace Nexus.Withdrawals.Aggregates;

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
    public const int MaxPixKeyLength = 200;
    public const int MaxLabelLength = 100;

    public string Id { get; }
    public string StrawManAccountId { get; }
    public BrazilianBank Bank { get; }
    public string Agency { get; private set; }
    public string AccountNumber { get; private set; }
    public string? AccountDigit { get; private set; }
    public BankAccountType AccountType { get; private set; }
    public string? PixKey { get; private set; }
    public string? Label { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    internal BankAccount(
        string Id,
        string StrawManAccountId,
        BrazilianBank Bank,
        string Agency,
        string AccountNumber,
        string? AccountDigit,
        BankAccountType AccountType,
        string? PixKey,
        string? Label,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        this.Id = Id;
        this.StrawManAccountId = StrawManAccountId;
        this.Bank = Bank;
        this.Agency = Agency;
        this.AccountNumber = AccountNumber;
        this.AccountDigit = AccountDigit;
        this.AccountType = AccountType;
        this.PixKey = PixKey;
        this.Label = Label;
        this.CreatedAt = CreatedAt;
        this.UpdatedAt = UpdatedAt;
    }

    public static IResult<BankAccount> Create(
        string strawManAccountId,
        BrazilianBank bank,
        string agency,
        string accountNumber,
        string? accountDigit,
        BankAccountType accountType,
        string? pixKey,
        string? label)
    {
        var builder = Result.Create<BankAccount>();

        strawManAccountId = strawManAccountId?.Trim() ?? string.Empty;
        agency = agency?.Trim() ?? string.Empty;
        accountNumber = accountNumber?.Trim() ?? string.Empty;
        accountDigit = string.IsNullOrWhiteSpace(accountDigit) ? null : accountDigit.Trim();
        pixKey = string.IsNullOrWhiteSpace(pixKey) ? null : pixKey.Trim();
        label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();

        if (string.IsNullOrWhiteSpace(strawManAccountId))
            builder.WithError(Error.Create()
                .WithCode(BankAccountErrorCodes.StrawManInvalid)
                .WithMessage("O ID do laranja é obrigatório.")
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

        if (pixKey is not null && pixKey.Length > MaxPixKeyLength)
            builder.WithError(Error.Create()
                .WithCode(BankAccountErrorCodes.PixKeyTooLong)
                .WithMessage($"A chave PIX pode ter no máximo {MaxPixKeyLength} caracteres.")
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
            Id: string.Empty,
            StrawManAccountId: strawManAccountId,
            Bank: bank,
            Agency: agency,
            AccountNumber: accountNumber,
            AccountDigit: accountDigit,
            AccountType: accountType,
            PixKey: pixKey,
            Label: label,
            CreatedAt: now,
            UpdatedAt: now)).Build();
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

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
