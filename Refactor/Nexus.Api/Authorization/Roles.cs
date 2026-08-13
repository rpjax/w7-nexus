namespace Refactor.Nexus.Api.Authorization;

/// <summary>
/// Preset raiz no hub. Demais papéis batizados (Operador, Laranja, Recrutador,
/// Gateways, Contador, Gestor de Operações) são <c>Mandato = capacidade × escopo</c>
/// e entram na etapa 02 — não são strings de role neste agregado.
/// Acionista é beneficiário, não mandato de gestão.
/// </summary>
public static class Roles
{
    public const string Administrator = "Administrator";

    public static bool IsGrantable(string? role) =>
        string.Equals(role?.Trim(), Administrator, StringComparison.OrdinalIgnoreCase);
}
