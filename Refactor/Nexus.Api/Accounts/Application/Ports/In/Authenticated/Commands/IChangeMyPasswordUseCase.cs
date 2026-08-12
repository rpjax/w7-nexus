using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Authenticated.Commands.ChangeMyPassword;

namespace Refactor.Nexus.Api.Accounts.Application.Ports.In.Authenticated.Commands;

public interface IChangeMyPasswordUseCase
{
    Task<IOperationResult<ChangeMyPasswordResult>> HandleAsync(
        ChangeMyPasswordCommand command,
        CancellationToken cancellationToken = default);
}
