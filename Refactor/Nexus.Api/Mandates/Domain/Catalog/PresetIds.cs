namespace Refactor.Nexus.Api.Mandates.Domain.Catalog;

public static class PresetIds
{
    public const string Recruiter = "Recruiter";
    public const string OperationsManager = "OperationsManager";
    public const string Accountant = "Accountant";
    public const string Gateways = "Gateways";
    public const string Operator = "Operator";
    public const string Orange = "Orange";

    private static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Recruiter,
        OperationsManager,
        Accountant,
        Gateways,
        Operator,
        Orange
    };

    public static bool IsKnown(string? presetId) =>
        !string.IsNullOrWhiteSpace(presetId) && All.Contains(presetId.Trim());

    public static string Normalize(string presetId) =>
        All.First(id => string.Equals(id, presetId.Trim(), StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyCollection<string> AllKnown => All;
}
