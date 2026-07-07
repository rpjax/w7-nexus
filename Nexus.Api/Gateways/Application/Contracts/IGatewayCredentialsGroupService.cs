using Aidan.Core.Patterns;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Application.Models;

namespace Nexus.Gateways.Application.Contracts;

public interface IGatewayCredentialsGroupService
{
    Task<IResult<GatewayCredentialsGroupDetails>> CreateGroupAsync(string name);
    Task<IResult> AssignGatewayCredentialsAsync(string groupId, string credentialsId);
    Task<IResult> UnassignGatewayCredentialsAsync(string groupId, string credentialsId);
    Task<IResult> DeleteGroupAsync(string groupId);
}
