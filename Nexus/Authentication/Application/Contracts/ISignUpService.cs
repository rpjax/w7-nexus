using Aidan.Core.Patterns;
using Nexus.Authentication.Application.Requests;
using Nexus.Authentication.Application.Responses;

namespace Nexus.Authentication.Application.Contracts;

public interface ISignUpService
{
    Task<IResult<SignUpResponse>> SignUpAsAdministratorAsync(SignUpRequest request);
    
    Task<IResult<SignUpResponse>> SignUpAsOperatorAsync(SignUpRequest request);
}
