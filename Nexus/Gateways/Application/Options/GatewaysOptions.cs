namespace Nexus.Gateways.Application.Options;

public sealed class GatewaysOptions
{
    public const string SectionName = "Gateways";

    /// <summary>
    /// When true, uses <see cref="Services.MockGatewayOrchestrator"/> instead of calling external gateway APIs.
    /// </summary>
    public bool UseMockOrchestrator { get; set; }
}
