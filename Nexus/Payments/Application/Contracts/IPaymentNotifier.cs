using Aidan.Core.Patterns;
using Nexus.Payments.Application.Services.Contracts;
using Nexus.Payments.Application.Models;

namespace Nexus.Payments.Application.Services.Contracts;

public interface IPaymentNotifier
{
    Task<IResult> NotifyStatusChangedAsync(NotifyStatusChangedRequest request);
}
