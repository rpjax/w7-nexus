using Aidan.Core.Patterns;
using Nexus.Legacy.Operations.Aggregates;
using Nexus.Legacy.Operations.Application.Models;

namespace Nexus.Legacy.Operations.Application;

public interface IOperationService
{
    Task<IResult<Operation>> CreateOperationAsync(CreateOperationRequest request);
    Task<IResult> AddOperatorAsync(string operationId, string operatorId);
    Task<IResult> RemoveOperatorAsync(string operationId, string operatorId);
    Task<IResult> AddStrawManAsync(string operationId, string strawManId);
    Task<IResult> RemoveStrawManAsync(string operationId, string strawManId);
    Task<IResult> EnableManualChargeCredentialsAsync(string operationId);
    Task<IResult> DisableManualChargeCredentialsAsync(string operationId);
    Task<IResult> AddChargeCredentialIdAsync(string operationId, string credentialId);
    Task<IResult> RemoveChargeCredentialIdAsync(string operationId, string credentialId);
    Task<IResult> DeleteOperationAsync(string operationId);
}
