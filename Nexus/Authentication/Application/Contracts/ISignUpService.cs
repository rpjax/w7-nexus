using Aidan.Core.Patterns;
using Nexus.Authentication.Application.Services.Requests;
using Nexus.Authentication.Application.Services.Responses;

namespace Nexus.Authentication.Application.Services.Contracts;

public interface ISignUpService
{
    Task<IResult<SignUpResponse>> SignUpAsAdministratorAsync(SignUpRequest request);
    
    Task<IResult<SignUpResponse>> SignUpAsOperatorAsync(SignUpRequest request);
}
