using Aidan.Core.Patterns;
using Nexus.Authentication.Application.Services.Requests;
using Nexus.Authentication.Application.Services.Responses;

namespace Nexus.Authentication.Application.Services.Contracts;

public interface ISignInService
{
    Task<IResult<SignInResponse>> SignInAsync(SignInRequest request);
}
