namespace Nexus.Authentication.Application.Contracts;

public interface IAdministratorSignUpTokenService
{
    bool IsAuthorized(string? authorizationHeader);
}
