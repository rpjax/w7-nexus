using Aidan.Core.Patterns;
using Nexus.Operations.Aggregates;

namespace Nexus.Operations.Application.Services.Contracts;

public interface IOperationRepository : IRepository<Operation>
{
    new Task<Operation> CreateAsync(Operation entity);
}
