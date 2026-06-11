using Aidan.Core.Errors;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authentication.Application.Services.Contracts;
using Nexus.Authentication.Application.Services.Requests;

namespace Nexus.Controllers.Authentication;

[Route("api/authentication")]
public class AuthenticationController : NexusController
{
    const string SignUpAsAdministratorToken = "";

    private ISignInService _signInService { get; }
    private ISignUpService _signUpService { get; }

    public AuthenticationController(
        ISignInService signInService,
        ISignUpService signUpService)
    {
        _signInService = signInService;
        _signUpService = signUpService;
    }

    [HttpPost("sign-up/administrator")]
    public async Task<ActionResult> SignUpAsAdministratorAsync([FromBody] SignUpRequest request)
    {
        var result = await _signUpService.SignUpAsAdministratorAsync(request);
        return ToResponse(result);
    }

    [HttpPost("sign-up/operator")]
    public async Task<ActionResult> SignUpAsOperatorAsync([FromBody] SignUpRequest request)
    {
        if (Request.Headers["Authorization"] != SignUpAsAdministratorToken)
        {
            return ProblemResponse(401, Error.Create()
                .WithCode("Authentication.UNAUTHORIZED")
                .WithMessage("Unauthorized")
                .Build());
        }

        var result = await _signUpService.SignUpAsOperatorAsync(request);
        return ToResponse(result);
    }

    [HttpPost("sign-in")]
    public async Task<ActionResult> SignInAsync([FromBody] SignInRequest request)
    {
        var result = await _signInService.SignInAsync(request);
        return ToResponse(result);
    }

}
