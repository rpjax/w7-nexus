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

public sealed class OlxAdministratorAdSpoofSearchService : IOlxAdministratorAdSpoofSearchService
{
    private readonly IAdSpoofRepository _adSpoofs;

    public OlxAdministratorAdSpoofSearchService(IAdSpoofRepository adSpoofs)
    {
        _adSpoofs = adSpoofs;
    }

    public async Task<IResult<SearchAdSpoofsResponse>> SearchAdSpoofsAsync(SearchAdSpoofsRequest request)
    {
        if (request is null)
            return AdSpoofSearchValidator.RequestBodyRequiredResult<SearchAdSpoofsResponse>();

        var validation = AdSpoofSearchValidator.Validate(request.Limit, request.Offset, request.Keyword);
        if (validation.IsFailure)
            return Result<SearchAdSpoofsResponse>.Failure(validation.Errors);

        var (limit, offset, keyword) = validation.Value;
        var operatorIds = AdSpoofSearchValidator.NormalizeFilterIds(request.OperatorIds);
        var operationIds = AdSpoofSearchValidator.NormalizeFilterIds(request.OperationIds);

        IAsyncQueryable<AdSpoof> query = _adSpoofs.AsQueryable();

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

        return Result<SearchAdSpoofsResponse>.Success(new SearchAdSpoofsResponse
        {
            Offset = offset,
            Limit = limit,
            Total = total,
            Items = page.Select(AdSpoofDetailsMapper.ToAdministratorDetails).ToList(),
        });
    }
}
