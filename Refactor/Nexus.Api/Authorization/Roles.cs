namespace Refactor.Nexus.Api.Authorization;

/// <summary>
/// Preset raiz no hub. Demais papéis batizados (Operador, Laranja, Recrutador,
/// Gateways, Contador, Gestor de Operações) vivem em <c>Mandates/</c>
/// como <c>Mandato = capacidade × escopo</c>. Acionista é beneficiário
/// (<c>ShareholderStake</c>), não mandato de gestão.
/// </summary>
public static class Roles
{
    public const string Administrator = "Administrator";

    public static bool IsGrantable(string? role) =>
        string.Equals(role?.Trim(), Administrator, StringComparison.OrdinalIgnoreCase);
}
