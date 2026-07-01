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

public sealed class OlxOperatorAdPatchSearchService : IOlxOperatorAdPatchSearchService
{
    private readonly IAdPatchRepository _adPatches;

    public OlxOperatorAdPatchSearchService(IAdPatchRepository adPatches)
    {
        _adPatches = adPatches;
    }

    public async Task<IResult<SearchAdPatchesResponse>> SearchAdPatchesAsync(
        RequesterIdentity identity,
        SearchAdPatchesRequest request)
    {
        if (request is null)
            return AdPatchSearchValidator.RequestBodyRequiredResult<SearchAdPatchesResponse>();

        var validation = AdPatchSearchValidator.Validate(request.Limit, request.Offset, request.Keyword);
        if (validation.IsFailure)
            return Result<SearchAdPatchesResponse>.Failure(validation.Errors);

        var (limit, offset, keyword) = validation.Value;
        var operationIds = AdPatchSearchValidator.NormalizeFilterIds(request.OperationIds);
        var operatorAccountId = identity.AccountId.Trim();

        IAsyncQueryable<AdPatch> query = _adPatches.AsQueryable()
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

        return Result<SearchAdPatchesResponse>.Success(new SearchAdPatchesResponse
        {
            Offset = offset,
            Limit = limit,
            Total = total,
            Items = page.Select(AdPatchDetailsMapper.ToOperatorDetails).ToList(),
        });
    }
}
