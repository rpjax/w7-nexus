using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Queries.GetAccountById;

namespace Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Queries;

public interface IGetAccountByIdUseCase
{
    Task<IOperationResult<GetAccountByIdResult>> HandleAsync(
        GetAccountByIdQuery query,
        CancellationToken cancellationToken = default);
}
