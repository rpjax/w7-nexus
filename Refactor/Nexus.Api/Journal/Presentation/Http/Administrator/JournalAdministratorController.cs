using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refactor.Nexus.Api.Journal.Application.UseCases.Administrator.Queries;
using Refactor.Nexus.Api.Shared.Controllers;

namespace Refactor.Nexus.Api.Journal.Presentation.Http.Administrator;

[Route("api/journal/administrator")]
[Authorize]
public sealed class JournalAdministratorController : ApiControllerBase
{
    private readonly IListJournalEntriesUseCase _list;

    public JournalAdministratorController(IListJournalEntriesUseCase list) => _list = list;

    [HttpGet("entries")]
    public async Task<ActionResult> ListAsync(
        [FromQuery] int? limit,
        [FromQuery] int offset,
        [FromQuery] string? type,
        CancellationToken cancellationToken) =>
        ToOperationResult(await _list.HandleAsync(new ListJournalEntriesQuery(limit, offset, type), cancellationToken));
}
