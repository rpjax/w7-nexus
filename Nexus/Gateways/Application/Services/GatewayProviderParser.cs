using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Errors;

namespace Nexus.Gateways.Application.Services;

public static class GatewayProviderParser
{
    public static IResult<PaymentGateway> TryParse(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return Result<PaymentGateway>.Failure(Error.Create()
                .WithCode(GatewayAdministratorErrorCodes.ProviderInvalid)
                .WithMessage("O provedor do gateway é obrigatório.")
                .Build());
        }

        return provider.Trim().ToLowerInvariant() switch
        {
            "frendz" => Result<PaymentGateway>.Success(PaymentGateway.Frendz),
            "wintech" => Result<PaymentGateway>.Success(PaymentGateway.Wintech),
            "sigilopay" => Result<PaymentGateway>.Success(PaymentGateway.SigiloPay),
            _ => Result<PaymentGateway>.Failure(Error.Create()
                .WithCode(GatewayAdministratorErrorCodes.ProviderInvalid)
                .WithMessage($"O provedor '{provider}' não é suportado.")
                .Build()),
        };
    }
}
