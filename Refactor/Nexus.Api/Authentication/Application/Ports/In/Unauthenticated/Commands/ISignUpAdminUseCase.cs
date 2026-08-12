using Refactor.Nexus.Api.Authentication.Application.UseCases.Unauthenticated.Commands.SignUpAdmin;

namespace Refactor.Nexus.Api.Authentication.Application.Ports.In.Unauthenticated.Commands;

public interface ISignUpAdminUseCase
{
    Task<IOperationResult<SignUpAdminResult>> HandleAsync(
        SignUpAdminCommand command,
        CancellationToken cancellationToken = default);
}
