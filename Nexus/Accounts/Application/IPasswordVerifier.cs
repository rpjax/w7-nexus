namespace Nexus.Accounts.Application;

public interface IPasswordVerifier
{
    Task<bool> VerifyAsync(string password, string passwordHash);
}
