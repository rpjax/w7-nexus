namespace Nexus.Frendz.Application;

public interface IFrendzApiKeysService
{
    Task<FredzApiCredentials?> GetRandomCredentialsAsync();

    Task<FredzApiCredentials> AddCredentialsAsync(string token, string name);

    Task<bool> DeleteCredentialsAsync(string id);
}
