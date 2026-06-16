using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Microsoft.AspNetCore.Http;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;
using Nexus.OperationAdministrator.Application.Contracts;
using Nexus.OperationAdministrator.Application.Requests;
using Nexus.OperationAdministrator.Application.Responses;
using Nexus.OperationAdministrator.Extensions;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Errors;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Nexus.OperationAdministrator.Application.Services;

public class OperationAdministrator : IOperationAdministrator
{
    private readonly string? _accountId;
    private readonly bool _isGlobalAdministrator;
    private ITeamService _teamService { get; }
    private IOperationRepository _operations { get; }
    private ITeamRepository _teams { get; }
    private IAccountRepository _accounts { get; }
    private ITeamGatewayDetailsLoader? _teamGatewayDetailsLoader { get; }
    private IHttpContextAccessor? _httpContextAccessor { get; }

    public OperationAdministrator(
        ITeamService teamService,
        IOperationRepository operations,
        ITeamRepository teams,
        IAccountRepository accounts,
        ITeamGatewayDetailsLoader teamGatewayDetailsLoader,
        IHttpContextAccessor httpContextAccessor)
    {
        _teamService = teamService;
        _operations = operations;
        _teams = teams;
        _accounts = accounts;
        _teamGatewayDetailsLoader = teamGatewayDetailsLoader;
        _httpContextAccessor = httpContextAccessor;
    }

    internal OperationAdministrator(
        string accountId,
        bool isGlobalAdministrator,
        ITeamService teamService,
        IOperationRepository operations,
        ITeamRepository teams,
        IAccountRepository accounts,
        ITeamGatewayDetailsLoader? teamGatewayDetailsLoader = null)
    {
        _accountId = accountId;
        _isGlobalAdministrator = isGlobalAdministrator;
        _teamService = teamService;
        _operations = operations;
        _teams = teams;
        _accounts = accounts;
        _teamGatewayDetailsLoader = teamGatewayDetailsLoader;
    }

    public async Task<IResult<SearchOperationsResponse>> SearchOperationsAsync(
        SearchOperationAdministratorOperationsRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<SearchOperationsResponse>();

        var (accountId, isGlobalAdministrator) = await ResolveAccountContextAsync();
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Result<SearchOperationsResponse>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.AdministratorInvalid)
                .WithMessage("A identidade do administrador de operação não foi encontrada.")
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

        var query = _operations.AsQueryable();

        if (!isGlobalAdministrator)
        {
            query = query.Where(o => o.AdministratorIds.Contains(accountId));
        }

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

    public async Task<IResult<CreateOperationTeamResponse>> CreateOperationTeamAsync(
        CreateOperationTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<CreateOperationTeamResponse>();

        var result = await _teamService.CreateTeamAsync(request.OperationId, request.Name);
        if (result.IsFailure)
            return Result<CreateOperationTeamResponse>.Failure(result.Errors);

        return Result<CreateOperationTeamResponse>.Success(new CreateOperationTeamResponse
        {
            Team = TeamDetailsMapper.Map(result.Value!)
        });
    }

    public async Task<IResult<DeleteOperationTeamResponse>> DeleteOperationTeamAsync(
        DeleteOperationTeamRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<DeleteOperationTeamResponse>();

        var result = await _teamService.DeleteTeamAsync(request.TeamId);
        return ToResponse<DeleteOperationTeamResponse>(result);
    }

    public async Task<IResult<AssignOperationTeamLeaderResponse>> AssignOperationTeamLeaderAsync(
        AssignOperationTeamLeaderRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<AssignOperationTeamLeaderResponse>();

        var result = await _teamService.AssignTeamLeaderAsync(request.TeamId, request.TeamLeaderId);
        return ToResponse<AssignOperationTeamLeaderResponse>(result);
    }

    public async Task<IResult<UnassignOperationTeamLeaderResponse>> UnassignOperationTeamLeaderAsync(
        UnassignOperationTeamLeaderRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<UnassignOperationTeamLeaderResponse>();

        var result = await _teamService.UnassignTeamLeaderAsync(request.TeamId);
        return ToResponse<UnassignOperationTeamLeaderResponse>(result);
    }

    private async Task<(string? AccountId, bool IsGlobalAdministrator)> ResolveAccountContextAsync()
    {
        if (!string.IsNullOrWhiteSpace(_accountId))
            return (_accountId.Trim(), _isGlobalAdministrator);

        var user = _httpContextAccessor?.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return (null, false);

        var accountId = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(accountId))
            return (null, false);

        var account = await _accounts.AsQueryable()
            .Where(a => a.Id == accountId)
            .FirstOrDefaultAsync();

        if (account is null)
            return (accountId.Trim(), false);

        return (accountId.Trim(), RoleAuthorization.IsGlobalAdministrator(account.Roles));
    }

    private static IResult<TResponse> ToResponse<TResponse>(Aidan.Core.Patterns.IResult result)
        where TResponse : new()
    {
        if (result.IsFailure)
            return Result<TResponse>.Failure(result.Errors);

        return Result<TResponse>.Success(new TResponse());
    }

    private static IResult<T> RequestBodyRequiredResult<T>()
    {
        return Result<T>.Failure(Error.Create()
            .WithCode(OperationErrorCodes.RequestBodyRequired)
            .WithMessage("O corpo da requisição é obrigatório.")
            .Build());
    }
}
