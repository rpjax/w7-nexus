namespace Nexus.Legacy.Accounts.Application;

public interface IPasswordHasher
{
    Task<string> HashAsync(string password);
}
