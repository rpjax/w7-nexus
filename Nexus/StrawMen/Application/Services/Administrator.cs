using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.StrawMen.Application.Contracts;

namespace Nexus.StrawMen.Application.Services;

public sealed class Administrator : IAdministrator
{
    private readonly IAdministratorAccessPolicy _policy;
    private readonly IAdministratorStrawManSettingsCommandService _strawManSettings;
    private readonly IAdministratorStrawManSettingsQueryService _strawManSettingsQuery;

    public Administrator(
        IAdministratorAccessPolicy policy,
        IAdministratorStrawManSettingsCommandService strawManSettings,
        IAdministratorStrawManSettingsQueryService strawManSettingsQuery)
    {
        _policy = policy;
        _strawManSettings = strawManSettings;
        _strawManSettingsQuery = strawManSettingsQuery;
    }

    public Task<IOperationResult<StrawManSettingsDetails>> GetStrawManSettingsAsync(
        RequesterIdentity identity,
        string strawManId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            () => _strawManSettingsQuery.GetStrawManSettingsAsync(strawManId),
            cancellationToken);
    }

    public Task<IOperationResult<StrawManSettingsDetails>> UpsertStrawManSettingsAsync(
        RequesterIdentity identity,
        string strawManId,
        decimal movementFeePercentage,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            () => _strawManSettings.UpsertStrawManSettingsAsync(identity, strawManId, movementFeePercentage),
            cancellationToken);
    }

    private async Task<IOperationResult<T>> ExecuteAsync<T>(
        RequesterIdentity identity,
        Func<Task<IResult<T>>> executeAsync,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

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
}
