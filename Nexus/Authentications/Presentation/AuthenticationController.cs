using Aidan.Core.Errors;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authentications.Application.Contracts;
using Nexus.Authentications.Application.Requests;
using Nexus.Controllers;

namespace Nexus.Authentications.Presentation;

[Route("api/authentication")]
public class AuthenticationController : NexusController
{
    private ISignInService _signInService { get; }
    private ISignUpService _signUpService { get; }
    private IAdministratorSignUpTokenService _administratorSignUpTokenService { get; }

    public AuthenticationController(
        ISignInService signInService,
        ISignUpService signUpService,
        IAdministratorSignUpTokenService administratorSignUpTokenService)
    {
        _signInService = signInService;
        _signUpService = signUpService;
        _administratorSignUpTokenService = administratorSignUpTokenService;
    }

    [HttpPost("sign-up/administrator")]
    public async Task<ActionResult> SignUpAsAdministratorAsync([FromBody] SignUpRequest request)
    {
        if (!_administratorSignUpTokenService.IsAuthorized(Request.Headers.Authorization))
        {
            return ProblemResponse(401, Error.Create()
                .WithCode("Authentication.UNAUTHORIZED")
                .WithMessage("Chave mestra inválida ou ausente. Verifique a autorização e tente novamente.")
                .Build());
        }

        var result = await _signUpService.SignUpAsAdministratorAsync(request);
        return ToResponse(result);
    }

    [HttpPost("sign-up/operator")]
    public async Task<ActionResult> SignUpAsOperatorAsync([FromBody] SignUpRequest request)
    {
        var result = await _signUpService.SignUpAsOperatorAsync(request);
        return ToResponse(result);
    }

    [HttpPost("sign-up/strawman")]
    public async Task<ActionResult> SignUpAsStrawManAsync([FromBody] SignUpRequest request)
    {
        var result = await _signUpService.SignUpAsStrawManAsync(request);
        return ToResponse(result);
    }

    [HttpPost("sign-in")]
    public async Task<ActionResult> SignInAsync([FromBody] SignInRequest request)
    {
        var result = await _signInService.SignInAsync(request);
        return ToResponse(result);
    }
}
