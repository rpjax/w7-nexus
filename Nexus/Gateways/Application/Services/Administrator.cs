using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Application.Requests.Administrator;
using Nexus.Gateways.Application.Responses.Administrator;
using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.Wintech.Application.Models;

namespace Nexus.Gateways.Application.Services;

public sealed class Administrator : IAdministrator
{
    private readonly IAdministratorAccessPolicy _policy;
    private readonly IAdministratorGatewayCredentialsSearchService _search;
    private readonly IAdministratorGatewayCredentialsCommandService _commands;

    public Administrator(
        IAdministratorAccessPolicy policy,
        IAdministratorGatewayCredentialsSearchService search,
        IAdministratorGatewayCredentialsCommandService commands)
    {
        _policy = policy;
        _search = search;
        _commands = commands;
    }

    public Task<IOperationResult<SearchGatewayCredentialsResponse>> SearchCredentialsAsync(
        RequesterIdentity identity,
        PaymentGateway provider,
        SearchGatewayCredentialsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            () => _search.SearchCredentialsAsync(provider, request),
            cancellationToken);
    }

    public Task<IOperationResult<FrendzApiCredentials>> AddFrendzCredentialsAsync(
        RequesterIdentity identity,
        AddCredentialsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(identity, () => _commands.AddFrendzCredentialsAsync(request), cancellationToken);
    }

    public Task<IOperationResult<bool>> UpdateFrendzCredentialsAsync(
        RequesterIdentity identity,
        UpdateCredentialsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteDeleteAsync(identity, () => _commands.UpdateFrendzCredentialsAsync(request), cancellationToken);
    }

    public Task<IOperationResult<bool>> SetFrendzCredentialEnabledAsync(
        RequesterIdentity identity,
        SetFrendzCredentialEnabledRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteDeleteAsync(identity, () => _commands.SetFrendzCredentialEnabledAsync(request), cancellationToken);
    }

    public Task<IOperationResult<bool>> DeleteFrendzCredentialsAsync(
        RequesterIdentity identity,
        string id,
        CancellationToken cancellationToken = default)
    {
        return ExecuteDeleteAsync(identity, () => _commands.DeleteFrendzCredentialsAsync(id), cancellationToken);
    }

    public Task<IOperationResult<WintechApiCredentials>> AddWintechCredentialsAsync(
        RequesterIdentity identity,
        AddWintechCredentialsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(identity, () => _commands.AddWintechCredentialsAsync(request), cancellationToken);
    }

    public Task<IOperationResult<bool>> UpdateWintechCredentialsAsync(
        RequesterIdentity identity,
        UpdateWintechCredentialsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteDeleteAsync(identity, () => _commands.UpdateWintechCredentialsAsync(request), cancellationToken);
    }

    public Task<IOperationResult<bool>> SetWintechCredentialEnabledAsync(
        RequesterIdentity identity,
        SetWintechCredentialEnabledRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteDeleteAsync(identity, () => _commands.SetWintechCredentialEnabledAsync(request), cancellationToken);
    }

    public Task<IOperationResult<bool>> DeleteWintechCredentialsAsync(
        RequesterIdentity identity,
        string id,
        CancellationToken cancellationToken = default)
    {
        return ExecuteDeleteAsync(identity, () => _commands.DeleteWintechCredentialsAsync(id), cancellationToken);
    }

    public Task<IOperationResult<SigiloPayApiCredentials>> AddSigiloPayCredentialsAsync(
        RequesterIdentity identity,
        AddSigiloPayCredentialsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(identity, () => _commands.AddSigiloPayCredentialsAsync(request), cancellationToken);
    }

    public Task<IOperationResult<bool>> UpdateSigiloPayCredentialsAsync(
        RequesterIdentity identity,
        UpdateSigiloPayCredentialsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteDeleteAsync(identity, () => _commands.UpdateSigiloPayCredentialsAsync(request), cancellationToken);
    }

    public Task<IOperationResult<bool>> SetSigiloPayCredentialEnabledAsync(
        RequesterIdentity identity,
        SetSigiloPayCredentialEnabledRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteDeleteAsync(identity, () => _commands.SetSigiloPayCredentialEnabledAsync(request), cancellationToken);
    }

    public Task<IOperationResult<bool>> DeleteSigiloPayCredentialsAsync(
        RequesterIdentity identity,
        string id,
        CancellationToken cancellationToken = default)
    {
        return ExecuteDeleteAsync(identity, () => _commands.DeleteSigiloPayCredentialsAsync(id), cancellationToken);
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

    private async Task<IOperationResult<bool>> ExecuteDeleteAsync(
        RequesterIdentity identity,
        Func<Task<IResult>> executeAsync,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<bool>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<bool>.Unauthorized(authorization.AuthorizationErrors);

        var result = await executeAsync();

        if (result.IsFailure)
            return OperationResult<bool>.Failure(result.Errors);

        return OperationResult<bool>.Success(true);
    }
}
