using Aidan.Core.Patterns;
using Nexus.Gateways.Aggregates;

namespace Nexus.Gateways.Application.Services.Contracts;

public interface IGatewayCredentialsGroupRepository : IRepository<GatewayCredentialsGroup>
{
    new Task<GatewayCredentialsGroup> CreateAsync(GatewayCredentialsGroup entity);
}
