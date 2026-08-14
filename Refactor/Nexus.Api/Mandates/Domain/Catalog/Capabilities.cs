namespace Refactor.Nexus.Api.Mandates.Domain.Catalog;

public static class Capabilities
{
    public const string ConcederMandato = "conceder_mandato";
    public const string Recrutar = "recrutar";
    public const string ConcederRecrutamento = "conceder_recrutamento";
    public const string Onboard = "onboard";
    public const string GerirOperacao = "gerir_operacao";
    public const string GerirGateways = "gerir_gateways";
    public const string RegistrarMovimentoFinanceiro = "registrar_movimento_financeiro";
    public const string VerFinanceiroAmplo = "ver_financeiro_amplo";
    public const string AtuarComoOperador = "atuar_como_operador";
    public const string AtuarComoLaranja = "atuar_como_laranja";
    public const string LerLogAuditoria = "ler_log_auditoria";

    private static readonly HashSet<string> All = new(StringComparer.Ordinal)
    {
        ConcederMandato,
        Recrutar,
        ConcederRecrutamento,
        Onboard,
        GerirOperacao,
        GerirGateways,
        RegistrarMovimentoFinanceiro,
        VerFinanceiroAmplo,
        AtuarComoOperador,
        AtuarComoLaranja,
        LerLogAuditoria
    };

    public static bool IsKnown(string? capability) =>
        !string.IsNullOrWhiteSpace(capability) && All.Contains(capability.Trim());

    public static IReadOnlyCollection<string> AllKnown => All;
}
