using Aidan.Core.Linq;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Olx.Aggregates;
using Nexus.Olx.Application.Contracts;
using Nexus.Olx.Application.Mapping;
using Nexus.Olx.Application.Requests.Administrator;
using Nexus.Olx.Application.Responses.Administrator;
using Nexus.Olx.Application.Services;

namespace Nexus.Olx.Application.Services;

public sealed class OlxAdministratorAdPatchSearchService : IOlxAdministratorAdPatchSearchService
{
    private readonly IAdPatchRepository _adPatches;

    public OlxAdministratorAdPatchSearchService(IAdPatchRepository adPatches)
    {
        _adPatches = adPatches;
    }

    public async Task<IResult<SearchAdPatchesResponse>> SearchAdPatchesAsync(SearchAdPatchesRequest request)
    {
        if (request is null)
            return AdPatchSearchValidator.RequestBodyRequiredResult<SearchAdPatchesResponse>();

        var validation = AdPatchSearchValidator.Validate(request.Limit, request.Offset, request.Keyword);
        if (validation.IsFailure)
            return Result<SearchAdPatchesResponse>.Failure(validation.Errors);

        var (limit, offset, keyword) = validation.Value;
        var operatorIds = AdPatchSearchValidator.NormalizeFilterIds(request.OperatorIds);
        var operationIds = AdPatchSearchValidator.NormalizeFilterIds(request.OperationIds);

        IAsyncQueryable<AdPatch> query = _adPatches.AsQueryable();

        if (operatorIds.Length > 0)
            query = query.Where(s => s.OperatorId != null && operatorIds.Contains(s.OperatorId));

        if (operationIds.Length > 0)
            query = query.Where(s => operationIds.Contains(s.OperationId));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.ToLowerInvariant();
            query = query.Where(s =>
                s.Id.ToLower().Contains(term) ||
                s.OperationId.ToLower().Contains(term) ||
                s.AdId.ToLower().Contains(term) ||
                s.AdUrl.ToLower().Contains(term) ||
                (s.OperatorId != null && s.OperatorId.ToLower().Contains(term)));
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
            Items = page.Select(AdPatchDetailsMapper.ToAdministratorDetails).ToList(),
        });
    }
}
