using Aidan.Core.Patterns;
using Nexus.Operations.Aggregates;

namespace Nexus.Operations.Application.Contracts;

public interface ITeamRepository : IRepository<Team>
{
    new Task<Team> CreateAsync(Team entity);
}
