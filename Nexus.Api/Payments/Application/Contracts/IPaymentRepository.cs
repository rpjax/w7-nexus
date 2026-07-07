using Aidan.Core.Patterns;
using Nexus.Payments.Aggregates;

namespace Nexus.Payments.Application.Contracts;

public interface IPaymentRepository : IRepository<Payment>
{
    new Task<Payment> CreateAsync(Payment entity);
}
