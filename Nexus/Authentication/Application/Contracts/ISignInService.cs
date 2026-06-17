using Aidan.Core.Patterns;
using Nexus.Authentication.Application.Requests;
using Nexus.Authentication.Application.Responses;

namespace Nexus.Authentication.Application.Contracts;

public interface ISignInService
{
    Task<IResult<SignInResponse>> SignInAsync(SignInRequest request);
}
