namespace Nexus.Authentication.Application.Services.Contracts;

public interface IAdministratorSignUpTokenService
{
    bool IsAuthorized(string? authorizationHeader);
}
