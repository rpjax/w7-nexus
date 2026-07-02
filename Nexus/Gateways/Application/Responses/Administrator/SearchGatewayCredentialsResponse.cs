namespace Nexus.Gateways.Application.Responses.Administrator;

public class SearchGatewayCredentialsResponse
{
    public int Total { get; init; }
    public object[] Items { get; init; } = Array.Empty<object>();
}
