using Nexus.Transfers.Application.Contracts;
using Nexus.Transfers.Application.Services;
using Nexus.Transfers.Infrastructure.Persistance;

namespace Nexus.Transfers.Composition;

public static class TransfersServiceCollectionExtensions
{
    public static IServiceCollection AddNexusTransfers(this IServiceCollection services)
    {
        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<IWithdrawalTransferUseCase, WithdrawalTransferUseCase>();
        services.AddScoped<IMovementTransferUseCase, MovementTransferUseCase>();
        services.AddScoped<IPayoutTransferUseCase, PayoutTransferUseCase>();
        services.AddScoped<ITransferTimelineQueryService, TransferTimelineQueryService>();
        services.AddScoped<ITransferRepository, MongoTransferRepository>();

        return services;
    }
}
