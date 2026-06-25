using Aidan.Core.Patterns;
using Nexus.CryptoWallets.Aggregates;

namespace Nexus.CryptoWallets.Application.Contracts;

public interface ICryptoWalletRepository : IRepository<CryptoWallet>
{
    new Task<CryptoWallet> CreateAsync(CryptoWallet entity);
}
