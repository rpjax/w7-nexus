using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.GrantAccountRole;

namespace Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Commands;

public interface IGrantAccountRoleUseCase
{
    Task<IOperationResult<GrantAccountRoleResult>> HandleAsync(
        GrantAccountRoleCommand command,
        CancellationToken cancellationToken = default);
}
