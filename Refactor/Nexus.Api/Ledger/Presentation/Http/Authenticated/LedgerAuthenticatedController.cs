using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Authenticated.Queries;
using Refactor.Nexus.Api.Shared.Controllers;

namespace Refactor.Nexus.Api.Ledger.Presentation.Http.Authenticated;

[Route("api/ledger/authenticated")]
[Authorize]
public sealed class LedgerAuthenticatedController : ApiControllerBase
{
    private readonly IGetMyStatementUseCase _statement;

    public LedgerAuthenticatedController(IGetMyStatementUseCase statement)
    {
        _statement = statement;
    }

    [HttpGet("statement")]
    public async Task<ActionResult> StatementAsync(CancellationToken cancellationToken) =>
        ToOperationResult(await _statement.HandleAsync(new GetMyStatementQuery(), cancellationToken));
}
