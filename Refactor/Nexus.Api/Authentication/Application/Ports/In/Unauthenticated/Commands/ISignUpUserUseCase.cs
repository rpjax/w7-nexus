using Refactor.Nexus.Api.Authentication.Application.UseCases.Unauthenticated.Commands.SignUpUser;

namespace Refactor.Nexus.Api.Authentication.Application.Ports.In.Unauthenticated.Commands;

public interface ISignUpUserUseCase
{
    Task<IOperationResult<SignUpUserResult>> HandleAsync(
        SignUpUserCommand command,
        CancellationToken cancellationToken = default);
}
