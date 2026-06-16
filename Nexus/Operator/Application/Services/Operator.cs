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
using Nexus.Payments.Application.Contracts;

namespace Nexus.Operator.Application.Services;

public class Operator : IOperator
{
    private readonly string? _operatorAccountId;
    private IOperationRepository _operations { get; }
    private ITeamRepository _teams { get; }
    private IAccountRepository _accounts { get; }
    private IPaymentRepository _payments { get; }
    private ITeamGatewayDetailsLoader? _teamGatewayDetailsLoader { get; }
    private IHttpContextAccessor? _httpContextAccessor { get; }

    public Operator(
        IOperationRepository operations,
        ITeamRepository teams,
        IAccountRepository accounts,
        IPaymentRepository payments,
        ITeamGatewayDetailsLoader teamGatewayDetailsLoader,
        IHttpContextAccessor httpContextAccessor)
    {
        _operations = operations;
        _teams = teams;
        _accounts = accounts;
        _payments = payments;
        _teamGatewayDetailsLoader = teamGatewayDetailsLoader;
        _httpContextAccessor = httpContextAccessor;
    }

    internal Operator(
        string operatorAccountId,
        IOperationRepository operations,
        ITeamRepository teams,
        IAccountRepository accounts,
        IPaymentRepository payments)
    {
        _operatorAccountId = operatorAccountId;
        _operations = operations;
        _teams = teams;
        _accounts = accounts;
        _payments = payments;
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

        var visibleOperationIds = await OperatorOperationResolver.ResolveOperationIdsAsync(
            operatorAccountId,
            _teams,
            _payments);

        if (visibleOperationIds.Length == 0)
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

        var query = _operations.AsQueryable()
            .Where(o => visibleOperationIds.Contains(o.Id));

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

        var items = await OperationDetailsMapper.MapManyAsync(operations, _teams, _accounts, _teamGatewayDetailsLoader);

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
