using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.CreateAccount;

namespace Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Commands;

public interface ICreateAccountUseCase
{
    Task<IOperationResult<CreateAccountResult>> HandleAsync(
        CreateAccountCommand command,
        CancellationToken cancellationToken = default);
}
