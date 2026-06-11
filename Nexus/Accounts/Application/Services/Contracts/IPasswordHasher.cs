namespace Nexus.Accounts.Application.Services.Contracts;

public interface IPasswordHasher
{
    Task<string> HashAsync(string password);
}
