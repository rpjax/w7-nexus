using Aidan.Core.Patterns;

namespace Nexus.Payments.Application;

public interface IPaymentNotifier
{
    Task<IResult> NotifyStatusChangedAsync(NotifyStatusChangedRequest request);
}
