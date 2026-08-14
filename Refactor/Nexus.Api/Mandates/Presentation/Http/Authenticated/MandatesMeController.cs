using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Authenticated.Queries.GetMyCarteira;
using Refactor.Nexus.Api.Shared.Controllers;

namespace Refactor.Nexus.Api.Mandates.Presentation.Http.Authenticated;

[Route("api/mandates/me")]
[Authorize]
public sealed class MandatesMeController : ApiControllerBase
{
    private readonly IGetMyCarteiraUseCase _getMyCarteira;

    public MandatesMeController(IGetMyCarteiraUseCase getMyCarteira)
    {
        _getMyCarteira = getMyCarteira;
    }

    [HttpGet("carteira")]
    public async Task<ActionResult> GetMyCarteiraAsync(CancellationToken cancellationToken) =>
        ToOperationResult(await _getMyCarteira.HandleAsync(cancellationToken));
}
