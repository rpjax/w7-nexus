namespace Nexus.Accounts.Application.Services.Contracts;

public interface IPasswordVerifier
{
    Task<bool> VerifyAsync(string password, string passwordHash);
}
