using Aidan.Core.Patterns;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Aggregates;

namespace Nexus.Operations.Application.Contracts;

public interface IOperationRepository : IRepository<Operation>
{
}
