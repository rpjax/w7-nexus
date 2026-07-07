using Aidan.Core.Patterns;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Models;

namespace Nexus.Payments.Application.Contracts;

public interface IPaymentNotifier
{
    Task<IResult> NotifyStatusChangedAsync(NotifyStatusChangedRequest request);
}
