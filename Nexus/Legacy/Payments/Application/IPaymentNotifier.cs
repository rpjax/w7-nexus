using Aidan.Core.Patterns;
using Nexus.Legacy.Payments.Application.Models;

namespace Nexus.Legacy.Payments.Application;

public interface IPaymentNotifier
{
    Task<IResult> NotifyStatusChangedAsync(NotifyStatusChangedRequest request);
}
