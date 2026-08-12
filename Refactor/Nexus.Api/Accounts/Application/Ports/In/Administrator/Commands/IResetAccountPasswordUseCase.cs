using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.ResetAccountPassword;

namespace Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Commands;

public interface IResetAccountPasswordUseCase
{
    Task<IOperationResult<ResetAccountPasswordResult>> HandleAsync(
        ResetAccountPasswordCommand command,
        CancellationToken cancellationToken = default);
}
