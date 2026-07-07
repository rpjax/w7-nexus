using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Nexus.Olx.Aggregates;

namespace Nexus.Olx.Application.Contracts;

public interface IAdPatchRepository : IRepository<AdPatch>
{
    new Task<AdPatch> CreateAsync(AdPatch entity);
}
