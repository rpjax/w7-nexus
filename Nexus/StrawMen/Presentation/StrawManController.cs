using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorization.Application.Contracts;
using Nexus.Controllers;
using Nexus.StrawMen.Application.Contracts;

namespace Nexus.StrawMen.Presentation;

[Route("api/straw-man")]
[Authorize]
public class StrawManController : NexusController
{
    private IStrawMan _strawMan { get; }
    private IRequesterIdentityResolver _identityResolver { get; }

    public StrawManController(
        IStrawMan strawMan,
        IRequesterIdentityResolver identityResolver)
    {
        _strawMan = strawMan;
        _identityResolver = identityResolver;
    }
}
