using Refactor.Nexus.Api.Authentication.Application.UseCases.Unauthenticated.Commands.SignIn;

namespace Refactor.Nexus.Api.Authentication.Application.Ports.In.Unauthenticated.Commands;

public interface ISignInUseCase
{
    Task<IOperationResult<SignInResult>> HandleAsync(
        SignInCommand command,
        CancellationToken cancellationToken = default);
}
