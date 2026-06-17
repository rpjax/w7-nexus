namespace Nexus.Authentications.Application.Contracts;

public interface IAdministratorSignUpTokenService
{
    bool IsAuthorized(string? authorizationHeader);
}
