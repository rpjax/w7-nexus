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
    public string OwnerId { get; }
    public BrazilianBank Bank { get; }
    public string Agency { get; private set; }
    public string AccountNumber { get; private set; }
    public string? AccountDigit { get; private set; }
    public BankAccountType AccountType { get; private set; }
    public string? Label { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    internal BankAccount(
        string id,
        string ownerId,
        BrazilianBank bank,
        string agency,
        string accountNumber,
        string? accountDigit,
        BankAccountType accountType,
        string? label,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        OwnerId = ownerId;
        Bank = bank;
        Agency = agency;
        AccountNumber = accountNumber;
        AccountDigit = accountDigit;
        AccountType = accountType;
        Label = label;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
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
            ownerId: ownerId,
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

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
