using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorizations.Application.Models;
using Nexus.Operators.Application.Contracts;
using Nexus.Operators.Application.Mapping;
using Nexus.Operators.Application.Requests;
using Nexus.Operators.Application.Responses;
using Nexus.Operators.Application.Responses.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Errors;

namespace Nexus.Operators.Application.Services;

public sealed class OperatorOperationSearchService : IOperatorOperationSearchService
{
    private IOperationRepository _operations { get; }
    private ITeamRepository _teams { get; }
    private IAccountRepository _accounts { get; }

    public OperatorOperationSearchService(
        IOperationRepository operations,
        ITeamRepository teams,
        IAccountRepository accounts)
    {
        _operations = operations;
        _teams = teams;
        _accounts = accounts;
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

        var assignedTeams = await OperatorOperationResolver.ResolveAssignedTeamsAsync(
            identity.AccountId,
            _teams);

        if (assignedTeams.Length == 0)
        {
            return builder
                .WithValue(new SearchOperationsResponse
                {
                    Offset = offset,
                    Limit = limit,
                    Total = 0,
                    Items = new List<OperationDetails>()
                })
                .Build();
        }

        var operationIds = assignedTeams
            .Select(t => t.OperationId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var operations = await _operations.AsQueryable()
            .Where(o => operationIds.Contains(o.Id))
            .ToArrayAsync();

        var operationsById = operations.ToDictionary(o => o.Id, StringComparer.Ordinal);

        var memberships = assignedTeams
            .Where(t => operationsById.ContainsKey(t.OperationId))
            .Select(t => new OperationTeamMembership(operationsById[t.OperationId], t))
            .ToList();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.ToLowerInvariant();
            memberships = memberships
                .Where(m =>
                    m.Operation.Id.ToLower().Contains(term) ||
                    m.Operation.Name.ToLower().Contains(term) ||
                    (m.Operation.Description != null && m.Operation.Description.ToLower().Contains(term)))
                .ToList();
        }

        var orderedMemberships = memberships
            .OrderByDescending(m => m.Operation.UpdatedAt)
            .ThenBy(m => m.Team.Name)
            .ToList();

        var total = orderedMemberships.Count;

        var page = orderedMemberships
            .Skip(offset)
            .Take(limit)
            .ToArray();

        var items = await OperationDetailsMapper.MapManyAsync(
            page,
            _accounts,
            identity.AccountId);

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
