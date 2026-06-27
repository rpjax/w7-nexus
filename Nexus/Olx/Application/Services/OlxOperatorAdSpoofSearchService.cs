using Aidan.Core.Linq;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Olx.Aggregates;
using Nexus.Olx.Application.Contracts;
using Nexus.Olx.Application.Mapping;
using Nexus.Olx.Application.Requests.Operator;
using Nexus.Olx.Application.Responses.Operator;

namespace Nexus.Olx.Application.Services;

public sealed class OlxOperatorAdSpoofSearchService : IOlxOperatorAdSpoofSearchService
{
    private readonly IAdSpoofRepository _adSpoofs;

    public OlxOperatorAdSpoofSearchService(IAdSpoofRepository adSpoofs)
    {
        _adSpoofs = adSpoofs;
    }

    public async Task<IResult<SearchAdSpoofsResponse>> SearchAdSpoofsAsync(
        RequesterIdentity identity,
        SearchAdSpoofsRequest request)
    {
        if (request is null)
            return AdSpoofSearchValidator.RequestBodyRequiredResult<SearchAdSpoofsResponse>();

        var validation = AdSpoofSearchValidator.Validate(request.Limit, request.Offset, request.Keyword);
        if (validation.IsFailure)
            return Result<SearchAdSpoofsResponse>.Failure(validation.Errors);

        var (limit, offset, keyword) = validation.Value;
        var operationIds = AdSpoofSearchValidator.NormalizeFilterIds(request.OperationIds);
        var operatorAccountId = identity.AccountId.Trim();

        IAsyncQueryable<AdSpoof> query = _adSpoofs.AsQueryable()
            .Where(s => s.OperatorId == operatorAccountId);

        if (operationIds.Length > 0)
            query = query.Where(s => operationIds.Contains(s.OperationId));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.ToLowerInvariant();
            query = query.Where(s =>
                s.Id.ToLower().Contains(term) ||
                s.OperationId.ToLower().Contains(term) ||
                s.AdId.ToLower().Contains(term) ||
                s.AdUrl.ToLower().Contains(term));
        }

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(s => s.UpdatedAt)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync();

        return Result<SearchAdSpoofsResponse>.Success(new SearchAdSpoofsResponse
        {
            Offset = offset,
            Limit = limit,
            Total = total,
            Items = page.Select(AdSpoofDetailsMapper.ToOperatorDetails).ToList(),
        });
    }
}
