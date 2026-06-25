using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Nexus.BankAccounts.Aggregates;

namespace Nexus.Database.Models;

public sealed class BankBalanceSplitRecord
{
    public string AccountId { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public decimal Amount { get; set; }
    public BankSplitKind SplitKind { get; set; }
}

public sealed class BankBalanceOriginRecord
{
    public string OperationId { get; set; } = string.Empty;
    public string? OperatorId { get; set; }
    public string StrawManId { get; set; } = string.Empty;
}

public sealed class BankBalanceRecord
{
    public string Id { get; set; } = string.Empty;
    public decimal AmountBrl { get; set; }
    public string TransferId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<BankBalanceSplitRecord> Splits { get; set; } = new();
    public List<string> AppliedStrawManFeeIds { get; set; } = new();
    public BankBalanceOriginRecord Origin { get; set; } = new();
}

public sealed class BankAccountRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string OwnerId { get; set; } = string.Empty;
    public BrazilianBank Bank { get; set; }
    public string Agency { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? AccountDigit { get; set; }
    public BankAccountType AccountType { get; set; }
    public string? Label { get; set; }

    public List<BankBalanceRecord> Balances { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
