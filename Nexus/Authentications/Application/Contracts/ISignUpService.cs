using Aidan.Core.Patterns;
using Nexus.Authentications.Application.Requests;
using Nexus.Authentications.Application.Responses;

namespace Nexus.Authentications.Application.Contracts;

public interface ISignUpService
{
    Task<IResult<SignUpResponse>> SignUpAsAdministratorAsync(SignUpRequest request);
    
    Task<IResult<SignUpResponse>> SignUpAsOperatorAsync(SignUpRequest request);

    Task<IResult<SignUpResponse>> SignUpAsStrawManAsync(SignUpRequest request);
}
