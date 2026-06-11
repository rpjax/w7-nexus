namespace Nexus.Accounts.Application;

// domain service to validate account ids
public interface IAccountIdValidator
{
    Task<bool> ExistsAsync(string accountId);
}
