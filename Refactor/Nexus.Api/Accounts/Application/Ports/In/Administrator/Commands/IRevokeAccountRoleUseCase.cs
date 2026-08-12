using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.RevokeAccountRole;

namespace Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Commands;

public interface IRevokeAccountRoleUseCase
{
    Task<IOperationResult<RevokeAccountRoleResult>> HandleAsync(
        RevokeAccountRoleCommand command,
        CancellationToken cancellationToken = default);
}
