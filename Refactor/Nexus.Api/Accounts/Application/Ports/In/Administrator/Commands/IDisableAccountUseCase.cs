using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.DisableAccount;

namespace Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Commands;

public interface IDisableAccountUseCase
{
    Task<IOperationResult<DisableAccountResult>> HandleAsync(
        DisableAccountCommand command,
        CancellationToken cancellationToken = default);
}
