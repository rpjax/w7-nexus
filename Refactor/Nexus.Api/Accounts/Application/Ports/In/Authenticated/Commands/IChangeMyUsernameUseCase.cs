using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Authenticated.Commands.ChangeMyUsername;

namespace Refactor.Nexus.Api.Accounts.Application.Ports.In.Authenticated.Commands;

public interface IChangeMyUsernameUseCase
{
    Task<IOperationResult<ChangeMyUsernameResult>> HandleAsync(
        ChangeMyUsernameCommand command,
        CancellationToken cancellationToken = default);
}
