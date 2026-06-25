using Aidan.Core.Patterns;
using Nexus.Transfers.Aggregates;

namespace Nexus.Transfers.Application.Contracts;

public interface ITransferRepository : IRepository<Transfer>
{
    new Task<Transfer> CreateAsync(Transfer entity);
}
