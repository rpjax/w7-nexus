using Microsoft.AspNetCore.SignalR;

namespace Nexus.Payments.Presentation;

public sealed class PaymentStatusHub : Hub
{
    private const string GroupPrefix = "payment:";

    public static string GroupNameForPayment(string paymentId) => $"{GroupPrefix}{paymentId}";

    public Task JoinPaymentAsync(string paymentId)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            return Task.CompletedTask;

        return Groups.AddToGroupAsync(Context.ConnectionId, GroupNameForPayment(paymentId.Trim()));
    }

    public Task LeavePaymentAsync(string paymentId)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            return Task.CompletedTask;

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNameForPayment(paymentId.Trim()));
    }
}
