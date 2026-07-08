namespace Nexus.Charges.Presentation;

[Route("api/charges/operation")]
public sealed class OperationController : NexusController
{
    [HttpPost("pix")]
    public async Task<ActionResult> CreatePixChargeAsync(
            [FromBody] CreatePixChargeRequest request,
            CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.CreatePixChargeAsync(
            identity,
            request,
            cancellationToken));
    }
}