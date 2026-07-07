using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Charges.Application.Contracts;
using Nexus.Charges.Application.Models;

namespace Nexus.Charges.Application.Services;

public sealed class Administrator : IAdministrator
{
    private readonly IAdministratorAccessPolicy _policy;
    private readonly IChargeService _chargeService;

    public Administrator(
        IAdministratorAccessPolicy policy,
        IChargeService chargeService)
    {
        _policy = policy;
        _chargeService = chargeService;
    }

    public Task<IOperationResult<CreatePixChargeResponse>> CreatePixChargeAsync(
        RequesterIdentity identity,
        CreatePixChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(identity, () => _chargeService.CreatePixChargeAsync(request), cancellationToken);
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
