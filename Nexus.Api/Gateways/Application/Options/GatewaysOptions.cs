namespace Nexus.Gateways.Application.Options;

public sealed class GatewaysOptions
{
    public const string SectionName = "Gateways";

    /// <summary>
    /// When true, <see cref="Services.GatewayOrchestrator"/> bypasses external gateway APIs and returns mock PIX data.
    /// </summary>
    public bool UseMockOrchestrator { get; set; }
}
