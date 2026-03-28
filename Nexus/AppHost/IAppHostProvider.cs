namespace Nexus.AppHost;

public interface IAppHostProvider
{
    /// <summary>Base URL configurada, ou null se não definida.</summary>
    string? BaseUrl { get; }

    /// <summary>Monta a URL de callback de webhook para o segmento de rota do gateway (ex.: <c>frendz</c> → .../api/frendz/webhook/callback).</summary>
    string GetWebhookCallbackUrl(string gatewayApiSegment);
}
