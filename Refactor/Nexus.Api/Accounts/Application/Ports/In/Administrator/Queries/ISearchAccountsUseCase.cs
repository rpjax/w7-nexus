using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Queries.SearchAccounts;

namespace Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Queries;

public interface ISearchAccountsUseCase
{
    Task<IOperationResult<SearchAccountsResult>> HandleAsync(
        SearchAccountsQuery query,
        CancellationToken cancellationToken = default);
}
