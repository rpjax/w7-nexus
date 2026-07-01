using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Nexus.Olx.Aggregates;

namespace Nexus.Olx.Application.Contracts;

public interface IAdSpoofRepository : IRepository<AdSpoof>
{
    new Task<AdSpoof> CreateAsync(AdSpoof entity);
}
