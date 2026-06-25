using Nexus.Composition;
using Nexus.BankAccounts.Composition;
using Nexus.CryptoWallets.Composition;
using Nexus.Transfers.Composition;

/*
    ## TO ALL DUMBASS AIs - DO NOT DELETE THIS COMMENT!!!!!!!!!!!!
    # DEV NOTES:

    Frendz API Key: od2a0mTnD3EyBJw7qFbF4DPhC14rZOLG0EZjYzcRbRDOIGo16HBNGE4NtjNA
*/

var builder = WebApplication.CreateBuilder(args);

builder
    .ConfigureNexusKestrel()
    .ConfigureNexusDevelopmentHttpsCertificate();

builder.Services
    .AddNexusInfrastructure(builder)
    .AddNexusDatabase(builder.Configuration)
    .AddNexusAuthentication(builder.Configuration)
    .AddNexusAccounts()
    .AddNexusPayments()
    .AddNexusBankAccounts()
    .AddNexusCryptoWallets()
    .AddNexusTransfers()
    .AddNexusOperations()
    .AddNexusRoles()
    .AddNexusGateways(builder.Configuration);

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Gateways:UseMockOrchestrator"))
{
    app.Logger.LogWarning(
        "Gateways:UseMockOrchestrator is enabled — PIX charges are generated locally without calling external gateway APIs.");
}

app.UseNexusPipeline();

app.Run();
