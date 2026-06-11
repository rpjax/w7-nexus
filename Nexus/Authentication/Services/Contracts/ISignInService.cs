using Aidan.Core.Patterns;
using Nexus.Authentication.Services.Requests;
using Nexus.Authentication.Services.Responses;

namespace Nexus.Authentication.Services.Contracts;

public interface ISignInService
{
    Task<IResult<SignInResponse>> SignInAsync(SignInRequest request);
}
