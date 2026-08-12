using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.GrantAccountPermission;

namespace Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Commands;

public interface IGrantAccountPermissionUseCase
{
    Task<IOperationResult<GrantAccountPermissionResult>> HandleAsync(
        GrantAccountPermissionCommand command,
        CancellationToken cancellationToken = default);
}
