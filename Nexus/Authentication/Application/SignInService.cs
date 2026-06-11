using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authentication.Application.Contracts;
using Nexus.Authentication.Application.Models;
using Nexus.Authentication.ErrorCodes;

namespace Nexus.Authentication.Application;

public sealed class SignInService : ISignInService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordVerifier _passwordVerifier;
    private readonly IJwtTokenService _jwtTokenService;

    public SignInService(
        IAccountRepository accountRepository,
        IPasswordVerifier passwordVerifier,
        IJwtTokenService jwtTokenService)
    {
        _accountRepository = accountRepository;
        _passwordVerifier = passwordVerifier;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<IResult<SignInResponse>> SignInAsync(SignInRequest request)
    {
        if (request is null)
            return RequestRequiredResult<SignInResponse>();

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<SignInResponse>.Failure(Error.Create()
                .WithCode(AuthenticationErrorCodes.InvalidCredentials)
                .WithMessage("Invalid username or password")
                .Build());
        }

        var account = _accountRepository.AsQueryable()
            .FirstOrDefault(a => a.Username == request.Username);

        if (account is null)
        {
            return Result<SignInResponse>.Failure(Error.Create()
                .WithCode(AuthenticationErrorCodes.InvalidCredentials)
                .WithMessage("Invalid username or password")
                .Build());
        }

        var passwordValid = await _passwordVerifier.VerifyAsync(request.Password, account.PasswordHash);
        if (!passwordValid)
        {
            return Result<SignInResponse>.Failure(Error.Create()
                .WithCode(AuthenticationErrorCodes.InvalidCredentials)
                .WithMessage("Invalid username or password")
                .Build());
        }

        var tokens = _jwtTokenService.GenerateTokens(ToTokenSubject(account));

        return Result<SignInResponse>.Success(new SignInResponse
        {
            Tokens = tokens
        });
    }

    private static JwtTokenSubject ToTokenSubject(Accounts.Aggregates.Account account)
    {
        return new JwtTokenSubject
        {
            AccountId = account.Id,
            Username = account.Username,
            Roles = account.Roles,
            Permissions = account.Permissions
        };
    }

    private static IResult<T> RequestRequiredResult<T>()
    {
        return Result<T>.Failure(Error.Create()
            .WithCode(AuthenticationErrorCodes.RequestRequired)
            .WithMessage("Request body is required")
            .Build());
    }
}
