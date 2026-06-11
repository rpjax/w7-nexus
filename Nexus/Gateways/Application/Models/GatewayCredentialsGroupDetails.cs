using Nexus.Gateways.Entities;

namespace Nexus.Gateways.Application.Models;

public class GatewayCredentialsGroupDetails
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    public static GatewayCredentialsGroupDetails FromGroup(GatewayCredentialsGroup group)
    {
        return new GatewayCredentialsGroupDetails
        {
            Id = group.Id,
            Name = group.Name
        };
    }
}
