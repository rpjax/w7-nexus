using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Actors.Contracts;
using Nexus.Actors.Requests;
using Nexus.Actors.Responses;
using Nexus.Actors.Responses.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application;
using Nexus.Operations.ErrorCodes;

namespace Nexus.Actors;

public class Administrator : IAdministrator
{
    private IOperationService _operationService { get; }
    private IOperationRepository _operations { get; }

    public Administrator(
        IOperationService operationService,
        IOperationRepository operations)
    {
        _operationService = operationService;
        _operations = operations;
    }

    public Task<IResult<OperationDetails>> CreateOperationAsync(
        CreateOperationRequest request)
    {
        if (request is null)
            return Task.FromResult(RequestBodyRequiredResult<OperationDetails>());

        return _operationService.CreateOperationAsync(
            name: request.Name,
            description: request.Description);
    }

    public async Task<IResult<SearchOperationsResponse>> SearchOperationsAsync(
        SearchOperationsRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<SearchOperationsResponse>();

        var builder = Result.Create<SearchOperationsResponse>();

        var limit = request.Limit <= 0 ? 20 : request.Limit;
        var offset = request.Offset;
        var keyword = request.Keyword?.Trim();

        if (limit < 1 || limit >= 1000)
        {
            builder.WithError(Error.Create()
                .WithCode(OperationErrorCodes.SearchLimitInvalid)
                .WithMessage("Limit must be between 1 and 999.")
                .Build());
        }

        if (offset < 0)
        {
            builder.WithError(Error.Create()
                .WithCode(OperationErrorCodes.SearchOffsetInvalid)
                .WithMessage("Offset cannot be negative.")
                .Build());
        }

        if (!string.IsNullOrWhiteSpace(keyword) && keyword.Length > Operation.MaxNameLength)
        {
            builder.WithError(Error.Create()
                .WithCode(OperationErrorCodes.SearchKeywordTooLong)
                .WithMessage($"Keyword can have at most {Operation.MaxNameLength} characters.")
                .Build());
        }

        if (builder.ContainsError)
            return builder.Build();

        var query = _operations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.ToLowerInvariant();
            query = query.Where(o =>
                o.Id.ToLower().Contains(term) ||
                o.Name.ToLower().Contains(term) ||
                (o.Description != null && o.Description.ToLower().Contains(term)));
        }

        var administratorIds = NormalizeFilterIds(request.AdministratorIds);
        if (administratorIds.Length > 0)
        {
            query = query.Where(o =>
                o.AdministratorIds.Any(id => administratorIds.Contains(id)));
        }

        var total = await query.CountAsync();

        var operations = await query
            .OrderByDescending(o => o.UpdatedAt)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync();

        var response = new SearchOperationsResponse
        {
            Offset = offset,
            Limit = limit,
            Total = total,
            Items = operations
                .Select(OperationDetails.FromOperation)
                .ToList()
        };

        return builder
            .WithValue(response)
            .Build();
    }

    public async Task<IResult<DeleteOperationResponse>> DeleteOperationAsync(
        DeleteOperationRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<DeleteOperationResponse>();

        var result = await _operationService.DeleteOperationAsync(request.OperationId);
        if (result.IsFailure)
            return Result<DeleteOperationResponse>.Failure(result.Errors);

        return Result<DeleteOperationResponse>.Success(new DeleteOperationResponse());
    }

    public async Task<IResult<AssignOperationAdministratorResponse>> AssignOperationAdministratorAsync(
        AssignOperationAdministratorRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<AssignOperationAdministratorResponse>();

        var result = await _operationService.AssignAdministratorAsync(
            request.OperationId,
            request.AdministratorId);

        if (result.IsFailure)
            return Result<AssignOperationAdministratorResponse>.Failure(result.Errors);

        return Result<AssignOperationAdministratorResponse>.Success(new AssignOperationAdministratorResponse());
    }

    public async Task<IResult<UnassignOperationAdministratorResponse>> UnassignOperationAdministratorAsync(
        UnassignOperationAdministratorRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<UnassignOperationAdministratorResponse>();

        var result = await _operationService.UnassignAdministratorAsync(
            request.OperationId,
            request.AdministratorId);

        if (result.IsFailure)
            return Result<UnassignOperationAdministratorResponse>.Failure(result.Errors);

        return Result<UnassignOperationAdministratorResponse>.Success(new UnassignOperationAdministratorResponse());
    }

    private static string[] NormalizeFilterIds(string[]? ids)
        => (ids ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static IResult<T> RequestBodyRequiredResult<T>()
    {
        return Result<T>.Failure(Error.Create()
            .WithCode(OperationErrorCodes.RequestBodyRequired)
            .WithMessage("Request body is required.")
            .Build());
    }
}
