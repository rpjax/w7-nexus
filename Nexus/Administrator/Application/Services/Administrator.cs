using Aidan.Core.Errors;
using Nexus.Operations.Application.Contracts;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Operations.Aggregates;
using Nexus.Administrator.Extensions;
using Nexus.Operations.Errors;
using Nexus.Accounts.Application.Contracts;
using Nexus.Accounts.Errors;
using Nexus.Administrator.Application.Contracts;
using Nexus.Administrator.Application.Responses;
using Nexus.Administrator.Application.Requests;
using Nexus.Administrator.Application.Responses.Models;
using Nexus.Authorization.Application.Models;

namespace Nexus.Administrator.Application.Services;

public class Administrator : IAdministrator
{
    private const int SearchKeywordMaxLength = 200;

    private IAdministratorAccessPolicy _policy { get; }
    private IOperationService _operationService { get; }
    private IOperationRepository _operations { get; }
    private IAccountRepository _accounts { get; }
    private ITeamRepository _teams { get; }
    private ITeamGatewayDetailsLoader _teamGatewayDetailsLoader { get; }

    public Administrator(
        IAdministratorAccessPolicy policy,
        IOperationService operationService,
        IOperationRepository operations,
        IAccountRepository accounts,
        ITeamRepository teams,
        ITeamGatewayDetailsLoader teamGatewayDetailsLoader)
    {
        _policy = policy;
        _operationService = operationService;
        _operations = operations;
        _accounts = accounts;
        _teams = teams;
        _teamGatewayDetailsLoader = teamGatewayDetailsLoader;
    }

    public Task<IOperationResult<OperationDetails>> CreateOperationAsync(
        RequesterIdentity identity,
        CreateOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => CreateOperationCoreAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<SearchOperationsResponse>> SearchOperationsAsync(
        RequesterIdentity identity,
        SearchOperationsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => SearchOperationsCoreAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<DeleteOperationResponse>> DeleteOperationAsync(
        RequesterIdentity identity,
        DeleteOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => DeleteOperationCoreAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<AssignOperationAdministratorResponse>> AssignOperationAdministratorAsync(
        RequesterIdentity identity,
        AssignOperationAdministratorRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => AssignOperationAdministratorCoreAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<UnassignOperationAdministratorResponse>> UnassignOperationAdministratorAsync(
        RequesterIdentity identity,
        UnassignOperationAdministratorRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => UnassignOperationAdministratorCoreAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<SearchAccountsResponse>> SearchAccountsAsync(
        RequesterIdentity identity,
        SearchAccountsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => SearchAccountsCoreAsync(request),
            cancellationToken);
    }

    private async Task<IOperationResult<T>> ExecuteAsync<T>(
        RequesterIdentity identity,
        Func<CancellationToken, Task<IAuthorizationResult>> authorizeAsync,
        Func<Task<IResult<T>>> executeAsync,
        CancellationToken cancellationToken)
    {
        var authorization = await authorizeAsync(cancellationToken);

        if (authorization.IsFailure)
            return OperationResult<T>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<T>.Unauthorized(authorization.AuthorizationErrors);

        var result = await executeAsync();

        if (result.IsFailure)
            return OperationResult<T>.Failure(result.Errors);

        if (result.Value is not T value)
            return OperationResult<T>.Failure(result.Errors);

        return OperationResult<T>.Success(value);
    }

    private async Task<IResult<OperationDetails>> CreateOperationCoreAsync(
        CreateOperationRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<OperationDetails>();

        var result = await _operationService.CreateOperationAsync(
            name: request.Name,
            description: request.Description);

        if (result.IsFailure)
            return Result<OperationDetails>.Failure(result.Errors);

        return Result<OperationDetails>.Success(result.Value!.ToOperationDetails());
    }

    private async Task<IResult<SearchOperationsResponse>> SearchOperationsCoreAsync(
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

    private async Task<IResult<DeleteOperationResponse>> DeleteOperationCoreAsync(
        DeleteOperationRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<DeleteOperationResponse>();

        var result = await _operationService.DeleteOperationAsync(request.OperationId);
        if (result.IsFailure)
            return Result<DeleteOperationResponse>.Failure(result.Errors);

        return Result<DeleteOperationResponse>.Success(new DeleteOperationResponse());
    }

    private async Task<IResult<AssignOperationAdministratorResponse>> AssignOperationAdministratorCoreAsync(
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

    private async Task<IResult<UnassignOperationAdministratorResponse>> UnassignOperationAdministratorCoreAsync(
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

    private async Task<IResult<SearchAccountsResponse>> SearchAccountsCoreAsync(
        SearchAccountsRequest request)
    {
        if (request is null)
            return AccountRequestBodyRequiredResult<SearchAccountsResponse>();

        var builder = Result.Create<SearchAccountsResponse>();

        var limit = request.Limit <= 0 ? 20 : request.Limit;
        var offset = request.Offset;
        var keyword = request.Keyword?.Trim();

        if (limit < 1 || limit >= 1000)
        {
            builder.WithError(Error.Create()
                .WithCode(AccountErrorCodes.SearchLimitInvalid)
                .WithMessage("O limite deve estar entre 1 e 999.")
                .Build());
        }

        if (offset < 0)
        {
            builder.WithError(Error.Create()
                .WithCode(AccountErrorCodes.SearchOffsetInvalid)
                .WithMessage("O deslocamento não pode ser negativo.")
                .Build());
        }

        if (!string.IsNullOrWhiteSpace(keyword) && keyword.Length > SearchKeywordMaxLength)
        {
            builder.WithError(Error.Create()
                .WithCode(AccountErrorCodes.SearchKeywordTooLong)
                .WithMessage($"A palavra-chave pode ter no máximo {SearchKeywordMaxLength} caracteres.")
                .Build());
        }

        if (builder.ContainsError)
            return builder.Build();

        var query = _accounts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.ToLowerInvariant();
            query = query.Where(a =>
                a.Id.ToLower().Contains(term) ||
                a.Username.ToLower().Contains(term));
        }

        var total = await query.CountAsync();

        var accounts = await query
            .OrderByDescending(a => a.LastUpdatedAt)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync();

        var response = new SearchAccountsResponse
        {
            Offset = offset,
            Limit = limit,
            Total = total,
            Items = accounts
                .Select(a => a.ToAccountDetails())
                .ToList()
        };

        return builder
            .WithValue(response)
            .Build();
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
            .WithMessage("O corpo da requisição é obrigatório.")
            .Build());
    }

    private static IResult<T> AccountRequestBodyRequiredResult<T>()
    {
        return Result<T>.Failure(Error.Create()
            .WithCode(AccountErrorCodes.RequestBodyRequired)
            .WithMessage("O corpo da requisição é obrigatório.")
            .Build());
    }
}
