using Aidan.Core.Patterns;
using Nexus.Charges.Application.Models;

namespace Nexus.Charges.Application;

public interface IGatewayCredentialsGroupService
{
    Task<IResult<GatewayCredentialsGroupDetails>> CreateGroupAsync(string name);
    Task<IResult> AssignGatewayCredentialsAsync(string groupId, string credentialsId);
    Task<IResult> UnassignGatewayCredentialsAsync(string groupId, string credentialsId);
    Task<IResult> DeleteGroupAsync(string groupId);
}
