using Aidan.Core.Patterns;
using Nexus.Payments.Application.Models;

namespace Nexus.Payments.Application;

public interface IPaymentNotifier
{
    Task<IResult> NotifyStatusChangedAsync(NotifyStatusChangedRequest request);
}
