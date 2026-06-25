using Aidan.Core.Patterns;
using Nexus.CryptoWallets.Aggregates;

namespace Nexus.CryptoWallets.Application.Contracts;

public interface ICryptoBalanceRepository : IRepository<CryptoBalance>
{
    new Task<CryptoBalance> CreateAsync(CryptoBalance entity);
}
