using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization.Application.Models;
using Nexus.OperationAdministrator.Application.Contracts;
using Nexus.OperationAdministrator.Application.Mapping;
using Nexus.OperationAdministrator.Application.Requests;
using Nexus.OperationAdministrator.Application.Responses;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Errors;

namespace Nexus.OperationAdministrator.Application.Services;

public sealed class OperationAdministratorOperationSearchService : IOperationAdministratorOperationSearchService
{
    private IOperationRepository _operations { get; }
    private ITeamRepository _teams { get; }
    private IAccountRepository _accounts { get; }
    private ITeamGatewayDetailsLoader _teamGatewayDetailsLoader { get; }

    public OperationAdministratorOperationSearchService(
        IOperationRepository operations,
        ITeamRepository teams,
        IAccountRepository accounts,
        ITeamGatewayDetailsLoader teamGatewayDetailsLoader)
    {
        _operations = operations;
        _teams = teams;
        _accounts = accounts;
        _teamGatewayDetailsLoader = teamGatewayDetailsLoader;
    }

    public async Task<IResult<SearchOperationsResponse>> SearchOperationsAsync(
        RequesterIdentity identity,
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

        var query = _operations.AsQueryable()
            .Where(o => o.AdministratorIds.Contains(identity.AccountId));

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

        var items = await OperationDetailsMapper.MapManyAsync(
            operations,
            _teams,
            _accounts,
            _teamGatewayDetailsLoader);

        var response = new SearchOperationsResponse
        {
            Offset = offset,
            Limit = limit,
            Total = total,
            Items = items.ToList()
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
