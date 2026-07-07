using Microsoft.Extensions.Options;
using Nexus.AppHost.Contracts;

namespace Nexus.AppHost;

public sealed class AppHostProvider : IAppHostProvider
{
    private readonly AppHostOptions _options;

    public AppHostProvider(IOptions<AppHostOptions> options)
    {
        _options = options.Value;
    }

    public string? BaseUrl
    {
        get
        {
            var raw = _options.BaseUrl?.Trim();
            if (string.IsNullOrEmpty(raw))
                return null;
            return raw.TrimEnd('/');
        }
    }

    public string GetWebhookCallbackUrl(string gatewayApiSegment)
    {
        if (string.IsNullOrWhiteSpace(gatewayApiSegment))
            throw new ArgumentException("Gateway segment is required.", nameof(gatewayApiSegment));

        var b = BaseUrl
            ?? throw new InvalidOperationException(
                "AppHost:BaseUrl is not configured. Set it in appsettings to build gateway webhook URLs.");

        var seg = gatewayApiSegment.Trim().Trim('/');
        return $"{b}/api/{seg}/webhook/callback";
    }
}
