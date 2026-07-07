using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorization.Application.Contracts;
using Nexus.Controllers;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Models;

namespace Nexus.Payments.Presentation;

[Route("api/payments/administrator")]
[Authorize]
public sealed class AdministratorController : NexusController
{
    private IAdministrator _administrator { get; }
    private IRequesterIdentityResolver _identityResolver { get; }

    public AdministratorController(
        IAdministrator administrator,
        IRequesterIdentityResolver identityResolver)
    {
        _administrator = administrator;
        _identityResolver = identityResolver;
    }

    [HttpPost("search")]
    public async Task<ActionResult> SearchPaymentsAsync(
        [FromBody] SearchPaymentsRequest? request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.SearchPaymentsAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpGet("{paymentId}")]
    public async Task<ActionResult> GetPaymentAsync(
        string paymentId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.GetPaymentAsync(
            identity,
            paymentId,
            cancellationToken));
    }

    [HttpPost("{paymentId}/pay")]
    public async Task<ActionResult> PayPaymentAsync(
        string paymentId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.PayPaymentAsync(
            identity,
            paymentId,
            cancellationToken));
    }

    [HttpPost("{paymentId}/refund")]
    public async Task<ActionResult> RefundPaymentAsync(
        string paymentId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.RefundPaymentAsync(
            identity,
            paymentId,
            cancellationToken));
    }

    [HttpPost("{paymentId}/kill")]
    public async Task<ActionResult> KillPaymentAsync(
        string paymentId,
        [FromBody] KillPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.KillPaymentAsync(
            identity,
            paymentId,
            request?.Reason ?? string.Empty,
            cancellationToken));
    }

    [HttpPost("{paymentId}/mark-distributed")]
    public async Task<ActionResult> MarkPaymentAsDistributedAsync(
        string paymentId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.MarkPaymentAsDistributedAsync(
            identity,
            paymentId,
            cancellationToken));
    }

    [HttpDelete("{paymentId}")]
    public async Task<ActionResult> DeletePaymentAsync(
        string paymentId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.DeletePaymentAsync(
            identity,
            paymentId,
            cancellationToken));
    }

    [HttpPost("{paymentId}/bind-operator")]
    public async Task<ActionResult> BindPaymentOperatorAsync(
        string paymentId,
        [FromBody] BindPaymentOperatorRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.BindPaymentOperatorAsync(
            identity,
            paymentId,
            request?.OperatorId ?? string.Empty,
            cancellationToken));
    }

    [HttpPost("{paymentId}/bind-straw-man")]
    public async Task<ActionResult> BindPaymentStrawManAsync(
        string paymentId,
        [FromBody] BindPaymentStrawManRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.BindPaymentStrawManAsync(
            identity,
            paymentId,
            request?.StrawManId ?? string.Empty,
            cancellationToken));
    }
}
