using Aidan.Core.Errors;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authentication.Application.Contracts;
using Nexus.Authentication.Application.Requests;
using Nexus.Controllers;

namespace Nexus.Api.Presentation.UnauthenticatedUsers;

[ApiController]
[Route("api/unauthenticated-users")]
public class UnauthenticatedUsersController : NexusController
{
    private ISignInService _signInService { get; }
    private ISignUpService _signUpService { get; }
    private IAdministratorSignUpTokenService _administratorSignUpTokenService { get; }

    public UnauthenticatedUsersController(
        ISignInService signInService,
        ISignUpService signUpService,
        IAdministratorSignUpTokenService administratorSignUpTokenService)
    {
        _signInService = signInService;
        _signUpService = signUpService;
        _administratorSignUpTokenService = administratorSignUpTokenService;
    }

    [HttpGet]
    public IActionResult GetUnauthenticatedUsers()
    {
        return Ok("Hello World");
    }
}