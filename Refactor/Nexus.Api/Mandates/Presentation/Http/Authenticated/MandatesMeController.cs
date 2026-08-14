using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Authenticated.Queries.GetMyCarteira;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Authenticated.Queries.GetMyMandate;
using Refactor.Nexus.Api.Shared.Controllers;

namespace Refactor.Nexus.Api.Mandates.Presentation.Http.Authenticated;

[Route("api/mandates/me")]
[Authorize]
public sealed class MandatesMeController : ApiControllerBase
{
    private readonly IGetMyCarteiraUseCase _getMyCarteira;
    private readonly IGetMyMandateUseCase _getMyMandate;

    public MandatesMeController(IGetMyCarteiraUseCase getMyCarteira, IGetMyMandateUseCase getMyMandate)
    {
        _getMyCarteira = getMyCarteira;
        _getMyMandate = getMyMandate;
    }

    [HttpGet("carteira")]
    public async Task<ActionResult> GetMyCarteiraAsync(CancellationToken cancellationToken) =>
        ToOperationResult(await _getMyCarteira.HandleAsync(cancellationToken));

    [HttpGet]
    public async Task<ActionResult> GetMyMandateAsync(CancellationToken cancellationToken) =>
        ToOperationResult(await _getMyMandate.HandleAsync(cancellationToken));
}
