using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.EnableAccount;

namespace Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Commands;

public interface IEnableAccountUseCase
{
    Task<IOperationResult<EnableAccountResult>> HandleAsync(
        EnableAccountCommand command,
        CancellationToken cancellationToken = default);
}
