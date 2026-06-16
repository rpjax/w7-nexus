using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Microsoft.AspNetCore.Http;
using Nexus.Accounts.Application.Contracts;
using Nexus.Operator.Application.Contracts;
using Nexus.Operator.Application.Requests;
using Nexus.Operator.Application.Responses;
using Nexus.Operator.Application.Responses.Models;
using Nexus.Operator.Extensions;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Errors;

namespace Nexus.Operator.Application.Services;

public class Operator : IOperator
{
    private readonly string? _operatorAccountId;
    private IOperationRepository _operations { get; }
    private ITeamRepository _teams { get; }
    private IAccountRepository _accounts { get; }
    private IHttpContextAccessor? _httpContextAccessor { get; }

    public Operator(
        IOperationRepository operations,
        ITeamRepository teams,
        IAccountRepository accounts,
        IHttpContextAccessor httpContextAccessor)
    {
        _operations = operations;
        _teams = teams;
        _accounts = accounts;
        _httpContextAccessor = httpContextAccessor;
    }

    internal Operator(
        string operatorAccountId,
        IOperationRepository operations,
        ITeamRepository teams,
        IAccountRepository accounts)
    {
        _operatorAccountId = operatorAccountId;
        _operations = operations;
        _teams = teams;
        _accounts = accounts;
    }

    public async Task<IResult<SearchOperationsResponse>> SearchOperationsAsync(
        SearchOperatorOperationsRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<SearchOperationsResponse>();

        var operatorAccountId = ResolveOperatorAccountId();
        if (string.IsNullOrWhiteSpace(operatorAccountId))
        {
            return Result<SearchOperationsResponse>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.OperatorInvalid)
                .WithMessage("A identidade do operador não foi encontrada.")
                .Build());
        }

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
            operatorAccountId,
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
            operatorAccountId);

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

    private string? ResolveOperatorAccountId()
    {
        if (!string.IsNullOrWhiteSpace(_operatorAccountId))
            return _operatorAccountId.Trim();

        var user = _httpContextAccessor?.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private static IResult<T> RequestBodyRequiredResult<T>()
    {
        return Result<T>.Failure(Error.Create()
            .WithCode(OperationErrorCodes.RequestBodyRequired)
            .WithMessage("O corpo da requisição é obrigatório.")
            .Build());
    }
}
