using System.Text.Json;
using Aidan.Core.Patterns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorization.Application.Contracts;
using Nexus.Authorization.Application.Models;
using Nexus.Controllers;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Application.Requests.Administrator;
using Nexus.Gateways.Application.Services;
using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.Frendz.Errors;
using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.SigiloPay.Errors;
using Nexus.Gateways.Wintech.Application.Models;
using Nexus.Gateways.Wintech.Errors;

namespace Nexus.Gateways.Presentation;

[Route("api/gateways/administrator")]
[Authorize]
public sealed class AdministratorController : NexusController
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private IAdministrator _administrator { get; }
    private IRequesterIdentityResolver _identityResolver { get; }

    public AdministratorController(
        IAdministrator administrator,
        IRequesterIdentityResolver identityResolver)
    {
        _administrator = administrator;
        _identityResolver = identityResolver;
    }

    [HttpPost("{provider}/search")]
    public async Task<ActionResult> SearchCredentialsAsync(
        string provider,
        [FromBody] SearchGatewayCredentialsRequest? request,
        CancellationToken cancellationToken)
    {
        var parseResult = GatewayProviderParser.TryParse(provider);
        if (parseResult.IsFailure)
            return ProblemResponse(422, parseResult.Errors);

        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.SearchCredentialsAsync(
            identity,
            parseResult.Value,
            request ?? new SearchGatewayCredentialsRequest(),
            cancellationToken));
    }

    [HttpPost("{provider}/credentials")]
    public async Task<ActionResult> AddCredentialsAsync(
        string provider,
        [FromBody] JsonElement body,
        CancellationToken cancellationToken)
    {
        var parseResult = GatewayProviderParser.TryParse(provider);
        if (parseResult.IsFailure)
            return ProblemResponse(422, parseResult.Errors);

        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return parseResult.Value switch
        {
            PaymentGateway.Frendz => ToOperationResult(await _administrator.AddFrendzCredentialsAsync(
                identity,
                Deserialize<AddCredentialsRequest>(body),
                cancellationToken)),
            PaymentGateway.Wintech => ToOperationResult(await _administrator.AddWintechCredentialsAsync(
                identity,
                Deserialize<AddWintechCredentialsRequest>(body),
                cancellationToken)),
            PaymentGateway.SigiloPay => ToOperationResult(await _administrator.AddSigiloPayCredentialsAsync(
                identity,
                Deserialize<AddSigiloPayCredentialsRequest>(body),
                cancellationToken)),
            _ => ProblemResponse(422, parseResult.Errors),
        };
    }

    [HttpPut("{provider}/credentials")]
    public async Task<ActionResult> UpdateCredentialsAsync(
        string provider,
        [FromBody] JsonElement body,
        CancellationToken cancellationToken)
    {
        var parseResult = GatewayProviderParser.TryParse(provider);
        if (parseResult.IsFailure)
            return ProblemResponse(422, parseResult.Errors);

        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        var operationResult = parseResult.Value switch
        {
            PaymentGateway.Frendz => await _administrator.UpdateFrendzCredentialsAsync(
                identity,
                Deserialize<UpdateCredentialsRequest>(body),
                cancellationToken),
            PaymentGateway.Wintech => await _administrator.UpdateWintechCredentialsAsync(
                identity,
                Deserialize<UpdateWintechCredentialsRequest>(body),
                cancellationToken),
            PaymentGateway.SigiloPay => await _administrator.UpdateSigiloPayCredentialsAsync(
                identity,
                Deserialize<UpdateSigiloPayCredentialsRequest>(body),
                cancellationToken),
            _ => null,
        };

        if (operationResult is null)
            return ProblemResponse(422, parseResult.Errors);

        return ToNoContentOperationResult(
            operationResult,
            FrendzErrorCodes.CredentialNotFound,
            WintechErrorCodes.CredentialNotFound,
            SigiloPayErrorCodes.CredentialNotFound);
    }

    [HttpPatch("{provider}/credentials/enabled")]
    public async Task<ActionResult> SetCredentialEnabledAsync(
        string provider,
        [FromBody] JsonElement body,
        CancellationToken cancellationToken)
    {
        var parseResult = GatewayProviderParser.TryParse(provider);
        if (parseResult.IsFailure)
            return ProblemResponse(422, parseResult.Errors);

        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        var operationResult = parseResult.Value switch
        {
            PaymentGateway.Frendz => await _administrator.SetFrendzCredentialEnabledAsync(
                identity,
                Deserialize<SetFrendzCredentialEnabledRequest>(body),
                cancellationToken),
            PaymentGateway.Wintech => await _administrator.SetWintechCredentialEnabledAsync(
                identity,
                Deserialize<SetWintechCredentialEnabledRequest>(body),
                cancellationToken),
            PaymentGateway.SigiloPay => await _administrator.SetSigiloPayCredentialEnabledAsync(
                identity,
                Deserialize<SetSigiloPayCredentialEnabledRequest>(body),
                cancellationToken),
            _ => null,
        };

        if (operationResult is null)
            return ProblemResponse(422, parseResult.Errors);

        return ToNoContentOperationResult(
            operationResult,
            FrendzErrorCodes.CredentialNotFound,
            WintechErrorCodes.CredentialNotFound,
            SigiloPayErrorCodes.CredentialNotFound);
    }

    [HttpDelete("{provider}/credentials")]
    public async Task<ActionResult> DeleteCredentialsAsync(
        string provider,
        [FromQuery] string id,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("O parâmetro de consulta id é obrigatório.");

        var parseResult = GatewayProviderParser.TryParse(provider);
        if (parseResult.IsFailure)
            return ProblemResponse(422, parseResult.Errors);

        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        var operationResult = parseResult.Value switch
        {
            PaymentGateway.Frendz => await _administrator.DeleteFrendzCredentialsAsync(identity, id, cancellationToken),
            PaymentGateway.Wintech => await _administrator.DeleteWintechCredentialsAsync(identity, id, cancellationToken),
            PaymentGateway.SigiloPay => await _administrator.DeleteSigiloPayCredentialsAsync(identity, id, cancellationToken),
            _ => null,
        };

        if (operationResult is null)
            return ProblemResponse(422, parseResult.Errors);

        return ToNoContentOperationResult(
            operationResult,
            FrendzErrorCodes.CredentialNotFound,
            WintechErrorCodes.CredentialNotFound,
            SigiloPayErrorCodes.CredentialNotFound);
    }

    private static T Deserialize<T>(JsonElement body)
        where T : class, new()
        => JsonSerializer.Deserialize<T>(body, JsonOptions) ?? new T();

    private ActionResult ToNoContentOperationResult(
        IOperationResult<bool> result,
        string frendzNotFoundCode,
        string wintechNotFoundCode,
        string sigiloPayNotFoundCode)
    {
        if (!result.IsAuthorized)
            return ProblemResponse(403, result.AuthorizationErrors);

        if (result.IsFailure)
        {
            if (result.Errors.Any(e =>
                    e.Code == frendzNotFoundCode
                    || e.Code == wintechNotFoundCode
                    || e.Code == sigiloPayNotFoundCode))
            {
                return ProblemResponse(404, result.Errors);
            }

            return ProblemResponse(422, result.Errors);
        }

        return NoContent();
    }
}
