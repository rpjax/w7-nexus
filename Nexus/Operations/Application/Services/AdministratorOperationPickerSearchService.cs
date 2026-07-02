using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Application.Requests.Administrator;
using Nexus.Operations.Application.Responses.Administrator;
using Nexus.Operations.Application.Responses.Administrator.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Errors;

namespace Nexus.Operations.Application.Services;

public sealed class AdministratorOperationPickerSearchService : IAdministratorOperationPickerSearchService
{
    private IOperationRepository _operations { get; }

    public AdministratorOperationPickerSearchService(IOperationRepository operations)
    {
        _operations = operations;
    }

    public async Task<IResult<SearchOperationsToAssignResponse>> SearchOperationsToAssignAsync(
        SearchOperationsToAssignRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<SearchOperationsToAssignResponse>();

        var builder = Result.Create<SearchOperationsToAssignResponse>();

        var limit = request.Limit <= 0 ? 20 : request.Limit;
        var offset = request.Offset;
        var keyword = request.Keyword?.Trim();

        if (limit < 1 || limit >= 1000)
        {
            builder.WithError(Error.Create()
                .WithCode(OperationErrorCodes.SearchLimitInvalid)
                .WithMessage("O limite deve estar entre 1 e 999.")
                .Build());
        }

        if (offset < 0)
        {
            builder.WithError(Error.Create()
                .WithCode(OperationErrorCodes.SearchOffsetInvalid)
                .WithMessage("O deslocamento não pode ser negativo.")
                .Build());
        }

        if (!string.IsNullOrWhiteSpace(keyword) && keyword.Length > Operation.MaxNameLength)
        {
            builder.WithError(Error.Create()
                .WithCode(OperationErrorCodes.SearchKeywordTooLong)
                .WithMessage($"A palavra-chave pode ter no máximo {Operation.MaxNameLength} caracteres.")
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

        var total = await query.CountAsync();

        var operations = await query
            .OrderByDescending(o => o.UpdatedAt)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync();

        var response = new SearchOperationsToAssignResponse
        {
            Offset = offset,
            Limit = limit,
            Total = total,
            Items = operations
                .Select(o => new OperationPickerDetails
                {
                    Id = o.Id,
                    Name = o.Name,
                })
                .ToList()
        };

        return builder
            .WithValue(response)
            .Build();
    }

    private static IResult<T> RequestBodyRequiredResult<T>()
    {
        return Result<T>.Failure(Error.Create()
            .WithCode(OperationErrorCodes.RequestBodyRequired)
            .WithMessage("O corpo da requisição é obrigatório.")
            .Build());
    }
}
