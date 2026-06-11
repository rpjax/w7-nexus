using Aidan.Core.Patterns;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Aggregates;

namespace Nexus.Payments.Application.Contracts;

public interface IPaymentRepository : IRepository<Payment>
{

}
