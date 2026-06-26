using Aidan.Core.Patterns;
using Nexus.Administrators.Application.Contracts;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Mapping;
using Nexus.Payments.Application.Models;

namespace Nexus.Administrators.Application.Services;

public sealed class AdministratorPaymentCommandService : IAdministratorPaymentCommandService
{
    private IPaymentService _payments { get; }
    private IPaymentDetailsEnrichmentService _enrichment { get; }

    public AdministratorPaymentCommandService(
        IPaymentService payments,
        IPaymentDetailsEnrichmentService enrichment)
    {
        _payments = payments;
        _enrichment = enrichment;
    }

    public async Task<IResult<PaymentDetails>> PayAndGetAsync(string paymentId)
    {
        var payResult = await _payments.PayAsync(paymentId);
        if (payResult.IsFailure)
            return Result<PaymentDetails>.Failure(payResult.Errors);

        return await LoadDetailsAsync(paymentId);
    }

    public async Task<IResult<PaymentDetails>> RefundAndGetAsync(string paymentId)
    {
        var refundResult = await _payments.RefundAsync(paymentId);
        if (refundResult.IsFailure)
            return Result<PaymentDetails>.Failure(refundResult.Errors);

        return await LoadDetailsAsync(paymentId);
    }

    public async Task<IResult<PaymentDetails>> KillAndGetAsync(string paymentId, string reason)
    {
        var killResult = await _payments.KillAsync(paymentId, reason);
        if (killResult.IsFailure)
            return Result<PaymentDetails>.Failure(killResult.Errors);

        return await LoadDetailsAsync(paymentId);
    }

    public async Task<IResult<PaymentDetails>> MarkAsDistributedAndGetAsync(string paymentId)
    {
        var markResult = await _payments.MarkAsDistributedAsync(paymentId);
        if (markResult.IsFailure)
            return Result<PaymentDetails>.Failure(markResult.Errors);

        return await LoadDetailsAsync(paymentId);
    }

    public Task<IResult> DeletePaymentAsync(string paymentId) =>
        _payments.DeletePaymentAsync(paymentId);

    public async Task<IResult<PaymentDetails>> BindOperatorAsync(string paymentId, string operatorAccountId)
    {
        var result = await _payments.BindOperatorAsync(paymentId, operatorAccountId);
        if (result.IsFailure)
            return Result<PaymentDetails>.Failure(result.Errors);

        return Result<PaymentDetails>.Success(
            await _enrichment.EnrichAsync(PaymentDetailsMapper.Map(result.Value!)));
    }

    public async Task<IResult<PaymentDetails>> BindStrawManAsync(string paymentId, string strawManAccountId)
    {
        var result = await _payments.BindStrawManAsync(paymentId, strawManAccountId);
        if (result.IsFailure)
            return Result<PaymentDetails>.Failure(result.Errors);

        return Result<PaymentDetails>.Success(
            await _enrichment.EnrichAsync(PaymentDetailsMapper.Map(result.Value!)));
    }

    private async Task<IResult<PaymentDetails>> LoadDetailsAsync(string paymentId)
    {
        var paymentResult = await _payments.GetByIdAsync(paymentId);
        if (paymentResult.IsFailure)
            return Result<PaymentDetails>.Failure(paymentResult.Errors);

        return Result<PaymentDetails>.Success(
            await _enrichment.EnrichAsync(PaymentDetailsMapper.Map(paymentResult.Value!)));
    }
}
