namespace Nexus.Accounts.Application;

public interface IPasswordHasher
{
    Task<string> HashAsync(string password);
}
