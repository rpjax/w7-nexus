using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refactor.Nexus.Api.Accounts.Application.Ports.In.Authenticated.Commands;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Authenticated.Commands.ChangeMyPassword;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Authenticated.Commands.ChangeMyUsername;
using Refactor.Nexus.Api.Authentication.Application.Ports.In.Authenticated.Queries;
using Refactor.Nexus.Api.Authentication.Application.Ports.In.Unauthenticated.Commands;
using Refactor.Nexus.Api.Authentication.Application.UseCases.Authenticated.Queries.GetMyProfile;
using Refactor.Nexus.Api.Authentication.Application.UseCases.Unauthenticated.Commands.SignIn;
using Refactor.Nexus.Api.Authentication.Application.UseCases.Unauthenticated.Commands.SignUpAdmin;
using Refactor.Nexus.Api.Authentication.Presentation.Http.Contracts;
using Refactor.Nexus.Api.Shared.Controllers;

namespace Refactor.Nexus.Api.Authentication.Presentation.Http;

[Route("api/authentication")]
public sealed class AuthenticationController : ApiControllerBase
{
    private const string AdministratorCreateTokenHeader = "X-Administrator-Create-Token";

    private readonly ISignUpAdminUseCase _signUpAdmin;
    private readonly ISignInUseCase _signIn;
    private readonly IGetMyProfileUseCase _getMyProfile;
    private readonly IChangeMyPasswordUseCase _changeMyPassword;
    private readonly IChangeMyUsernameUseCase _changeMyUsername;

    public AuthenticationController(
        ISignUpAdminUseCase signUpAdmin,
        ISignInUseCase signIn,
        IGetMyProfileUseCase getMyProfile,
        IChangeMyPasswordUseCase changeMyPassword,
        IChangeMyUsernameUseCase changeMyUsername)
    {
        _signUpAdmin = signUpAdmin;
        _signIn = signIn;
        _getMyProfile = getMyProfile;
        _changeMyPassword = changeMyPassword;
        _changeMyUsername = changeMyUsername;
    }

    [HttpPost("sign-up/admin")]
    public async Task<ActionResult> SignUpAdminAsync(
        [FromBody] SignUpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _signUpAdmin.HandleAsync(
            new SignUpAdminCommand(
                request.Username,
                request.Password,
                Request.Headers[AdministratorCreateTokenHeader].FirstOrDefault()),
            cancellationToken);

        return ToOperationResult(result);
    }

    [HttpPost("sign-in")]
    public async Task<ActionResult> SignInAsync(
        [FromBody] SignInRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _signIn.HandleAsync(
            new SignInCommand(request.Username, request.Password),
            cancellationToken);

        return ToOperationResult(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult> GetMyProfileAsync(CancellationToken cancellationToken)
    {
        var result = await _getMyProfile.HandleAsync(new GetMyProfileQuery(), cancellationToken);
        return ToOperationResult(result);
    }

    [Authorize]
    [HttpPost("me/password")]
    public async Task<ActionResult> ChangeMyPasswordAsync(
        [FromBody] ChangeMyPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _changeMyPassword.HandleAsync(
            new ChangeMyPasswordCommand(request.CurrentPassword, request.NewPassword),
            cancellationToken);

        return ToOperationResult(result);
    }

    [Authorize]
    [HttpPost("me/username")]
    public async Task<ActionResult> ChangeMyUsernameAsync(
        [FromBody] ChangeMyUsernameRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _changeMyUsername.HandleAsync(
            new ChangeMyUsernameCommand(request.NewUsername),
            cancellationToken);

        return ToOperationResult(result);
    }
}
