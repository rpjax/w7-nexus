namespace Nexus.Accounts.Application.Contracts;

public interface IPasswordHasher
{
    Task<string> HashAsync(string password);
}
