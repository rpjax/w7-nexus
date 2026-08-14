namespace Refactor.Nexus.Api.Ledger.Domain;

public static class AttritionCause
{
    public const string BloqueioBancario = "bloqueio_bancario";
    public const string Apreensao = "apreensao";
    public const string Traicao = "traicao";
    public const string SaidaVoluntaria = "saida_voluntaria";
    public const string ErroOperacional = "erro_operacional";
    public const string Estorno = "estorno";
    public const string Desconhecido = "desconhecido";

    private static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        BloqueioBancario,
        Apreensao,
        Traicao,
        SaidaVoluntaria,
        ErroOperacional,
        Estorno,
        Desconhecido
    };

    public static bool TryNormalize(string? cause, out string normalized)
    {
        normalized = (cause ?? "").Trim().ToLowerInvariant();
        return Known.Contains(normalized);
    }
}
