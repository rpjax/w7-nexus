using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Actors.Contracts;
using Nexus.Actors.Requests;
using Nexus.Authentication.Application.Contracts;
using Nexus.Authentication.Application.Models;
using Nexus.Authentication.ErrorCodes;
using Nexus.Authentication.Services.Requests;
using Nexus.Authentication.Services.Responses;

namespace Nexus.Authentication.Application;

public sealed class SignUpService : ISignUpService
{
    private readonly IUnauthenticatedUser _unauthenticatedUser;
    private readonly IAccountRepository _accountRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public SignUpService(
        IUnauthenticatedUser unauthenticatedUser,
        IAccountRepository accountRepository,
        IJwtTokenService jwtTokenService)
    {
        _unauthenticatedUser = unauthenticatedUser;
        _accountRepository = accountRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<IResult<SignUpResponse>> SignUpAsAdministratorAsync(SignUpRequest request)
    {
        if (request is null)
            return RequestRequiredResult<SignUpResponse>();

        var createResult = await _unauthenticatedUser.CreateAdministratorAccountAsync(
            new CreateAdministratorAccountRequest
            {
                Username = request.Username,
                Password = request.Password
            });

        return await BuildSignUpResponseAsync(createResult, request.Username);
    }

    public async Task<IResult<SignUpResponse>> SignUpAsOperatorAsync(SignUpRequest request)
    {
        if (request is null)
            return RequestRequiredResult<SignUpResponse>();

        var createResult = await _unauthenticatedUser.CreateOperatorAccountAsync(
            new CreateOperatorAccountRequest
            {
                Username = request.Username,
                Password = request.Password
            });

        return await BuildSignUpResponseAsync(createResult, request.Username);
    }

    private Task<IResult<SignUpResponse>> BuildSignUpResponseAsync<TActorResponse>(
        IResult<TActorResponse> createResult,
        string username)
    {
        if (createResult.IsFailure)
            return Task.FromResult<IResult<SignUpResponse>>(Result<SignUpResponse>.Failure(createResult.Errors));

        var account = _accountRepository.AsQueryable()
            .FirstOrDefault(a => a.Username == username);

        if (account is null)
        {
            return Task.FromResult<IResult<SignUpResponse>>(Result<SignUpResponse>.Failure(Error.Create()
                .WithCode(AuthenticationErrorCodes.AccountNotFound)
                .WithMessage("Account was created but could not be loaded")
                .Build()));
        }

        var tokens = _jwtTokenService.GenerateTokens(new JwtTokenSubject
        {
            AccountId = account.Id,
            Username = account.Username,
            Roles = account.Roles,
            Permissions = account.Permissions
        });

        return Task.FromResult<IResult<SignUpResponse>>(Result<SignUpResponse>.Success(new SignUpResponse
        {
            AccountId = account.Id,
            Tokens = tokens
        }));
    }

    private static IResult<T> RequestRequiredResult<T>()
    {
        return Result<T>.Failure(Error.Create()
            .WithCode(AuthenticationErrorCodes.RequestRequired)
            .WithMessage("Request body is required")
            .Build());
    }
}
