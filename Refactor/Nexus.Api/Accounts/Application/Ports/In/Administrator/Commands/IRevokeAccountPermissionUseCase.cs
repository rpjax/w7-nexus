using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.RevokeAccountPermission;

namespace Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Commands;

public interface IRevokeAccountPermissionUseCase
{
    Task<IOperationResult<RevokeAccountPermissionResult>> HandleAsync(
        RevokeAccountPermissionCommand command,
        CancellationToken cancellationToken = default);
}
