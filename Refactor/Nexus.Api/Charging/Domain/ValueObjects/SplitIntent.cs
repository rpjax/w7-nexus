namespace Refactor.Nexus.Api.Charging.Domain.ValueObjects;

public sealed record SplitParticipant(Guid MemberId, decimal PercentOfLineBase);

public sealed record SplitLine(
    int Order,
    string Kind,
    decimal PercentOfRemainder,
    IReadOnlyList<SplitParticipant> Participants);

public sealed record SplitIntent(IReadOnlyList<SplitLine> Lines)
{
    public const string Orange = "Orange";
    public const string Shareholders = "Shareholders";
    public const string OperationManagement = "OperationManagement";
    public const string Agency = "Agency";
    public const string ResidualOrg = "ResidualOrg";
}
