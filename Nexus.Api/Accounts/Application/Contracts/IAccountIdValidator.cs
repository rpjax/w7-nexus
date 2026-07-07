namespace Nexus.Accounts.Application.Contracts;

// domain service to validate account ids
public interface IAccountIdValidator
{
    Task<bool> ExistsAsync(string accountId);
}
