namespace Nexus.Legacy.Accounts.Application;

public interface IPasswordVerifier
{
    Task<bool> VerifyAsync(string password, string passwordHash);
}
