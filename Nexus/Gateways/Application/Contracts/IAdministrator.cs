using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Application.Requests.Administrator;
using Nexus.Gateways.Application.Responses.Administrator;
using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.Wintech.Application.Models;

namespace Nexus.Gateways.Application.Contracts;

public interface IAdministrator
{
    Task<IOperationResult<SearchGatewayCredentialsResponse>> SearchCredentialsAsync(
        RequesterIdentity identity,
        PaymentGateway provider,
        SearchGatewayCredentialsRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<FrendzApiCredentials>> AddFrendzCredentialsAsync(
        RequesterIdentity identity,
        AddCredentialsRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<bool>> UpdateFrendzCredentialsAsync(
        RequesterIdentity identity,
        UpdateCredentialsRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<bool>> SetFrendzCredentialEnabledAsync(
        RequesterIdentity identity,
        SetFrendzCredentialEnabledRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<bool>> DeleteFrendzCredentialsAsync(
        RequesterIdentity identity,
        string id,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<WintechApiCredentials>> AddWintechCredentialsAsync(
        RequesterIdentity identity,
        AddWintechCredentialsRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<bool>> UpdateWintechCredentialsAsync(
        RequesterIdentity identity,
        UpdateWintechCredentialsRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<bool>> SetWintechCredentialEnabledAsync(
        RequesterIdentity identity,
        SetWintechCredentialEnabledRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<bool>> DeleteWintechCredentialsAsync(
        RequesterIdentity identity,
        string id,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<SigiloPayApiCredentials>> AddSigiloPayCredentialsAsync(
        RequesterIdentity identity,
        AddSigiloPayCredentialsRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<bool>> UpdateSigiloPayCredentialsAsync(
        RequesterIdentity identity,
        UpdateSigiloPayCredentialsRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<bool>> SetSigiloPayCredentialEnabledAsync(
        RequesterIdentity identity,
        SetSigiloPayCredentialEnabledRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<bool>> DeleteSigiloPayCredentialsAsync(
        RequesterIdentity identity,
        string id,
        CancellationToken cancellationToken = default);
}
