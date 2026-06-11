using Aidan.Core.Patterns;
using Nexus.Authentication.Application.Models;

namespace Nexus.Authentication.Application.Contracts;

public interface ISignInService
{
    Task<IResult<SignInResponse>> SignInAsync(SignInRequest request);
}
