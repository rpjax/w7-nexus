using Aidan.Core.Patterns;
using Nexus.Authentications.Application.Requests;
using Nexus.Authentications.Application.Responses;

namespace Nexus.Authentications.Application.Contracts;

public interface ISignInService
{
    Task<IResult<SignInResponse>> SignInAsync(SignInRequest request);
}
