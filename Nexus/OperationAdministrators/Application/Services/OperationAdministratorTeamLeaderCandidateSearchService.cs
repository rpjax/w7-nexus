using Aidan.Core.Patterns;
using Nexus.OperationAdministrators.Application.Contracts;
using Nexus.OperationAdministrators.Application.Requests;
using Nexus.OperationAdministrators.Application.Responses;

namespace Nexus.OperationAdministrators.Application.Services;

public sealed class OperationAdministratorTeamLeaderCandidateSearchService
    : IOperationAdministratorTeamLeaderCandidateSearchService
{
    private IOperationAdministratorAccountSearchService _accountSearch { get; }

    public OperationAdministratorTeamLeaderCandidateSearchService(
        IOperationAdministratorAccountSearchService accountSearch)
    {
        _accountSearch = accountSearch;
    }

    public async Task<IResult<SearchTeamLeaderCandidatesResponse>> SearchTeamLeaderCandidatesAsync(
        SearchTeamLeaderCandidatesRequest request)
    {
        var result = await _accountSearch.SearchAccountsAsync(new SearchAccountsRequest
        {
            Limit = request?.Limit ?? 0,
            Offset = request?.Offset ?? 0,
            Keyword = request?.Keyword
        });

        if (result.IsFailure)
            return Result<SearchTeamLeaderCandidatesResponse>.Failure(result.Errors);

        if (result.Value is not SearchAccountsResponse accountsResponse)
            return Result<SearchTeamLeaderCandidatesResponse>.Failure(result.Errors);

        return Result<SearchTeamLeaderCandidatesResponse>.Success(new SearchTeamLeaderCandidatesResponse
        {
            Offset = accountsResponse.Offset,
            Limit = accountsResponse.Limit,
            Total = accountsResponse.Total,
            Items = accountsResponse.Items
        });
    }
}
