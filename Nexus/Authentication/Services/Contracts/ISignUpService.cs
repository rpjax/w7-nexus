using Aidan.Core.Patterns;
using Nexus.Authentication.Services.Requests;
using Nexus.Authentication.Services.Responses;

namespace Nexus.Authentication.Application.Contracts;

public interface ISignUpService
{
    Task<IResult<SignUpResponse>> SignUpAsAdministratorAsync(SignUpRequest request);
    
    Task<IResult<SignUpResponse>> SignUpAsOperatorAsync(SignUpRequest request);
}
