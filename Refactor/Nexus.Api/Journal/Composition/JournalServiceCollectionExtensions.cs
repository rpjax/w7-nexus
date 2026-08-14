using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Refactor.Nexus.Api.Infrastructure.Persistence;
using Refactor.Nexus.Api.Journal.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Journal.Application.UseCases.Administrator.Queries;
using Refactor.Nexus.Api.Journal.Infrastructure.Mandates;
using Refactor.Nexus.Api.Journal.Services;
using Refactor.Nexus.Api.Journal.Services.Contracts;
using Refactor.Nexus.Api.Journal.Storage;

namespace Refactor.Nexus.Api.Journal.Composition;

public static class JournalServiceCollectionExtensions
{
    /// <summary>
    /// Registers Journal admission, drain, health, metrics, live feed, and Postgres store
    /// on the shared Nexus database. Requires <c>INpgsqlConnectionFactory</c> (e.g. via
    /// <c>AddRefactorAccounts</c>). Call <see cref="DiscoverJournalFacts"/> to scan facts,
    /// then <see cref="JournalDatabaseInitializerExtensions.InitializeJournalDatabaseAsync"/> after build.
    /// </summary>
    public static IServiceCollection AddJournal(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (!services.Any(d => d.ServiceType == typeof(INpgsqlConnectionFactory)))
        {
            throw new InvalidOperationException(
                "AddJournal requires INpgsqlConnectionFactory. Call AddRefactorAccounts() (or register the factory) first.");
        }

        services.AddOptions<JournalDrainOptions>()
            .BindConfiguration(JournalDrainOptions.SectionName)
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<JournalDrainOptions>, JournalDrainOptionsValidator>());

        services.AddDbContext<JournalDbContext>((sp, options) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            options.UseNpgsql(NexusDbConnection.Resolve(configuration));
        });

        // Instance registration so DiscoverJournalFacts can mutate assemblies before Build.
        services.TryAddSingleton(new JournalFactDiscovery());

        services.TryAddSingleton<JournalCatalog>(sp =>
        {
            var catalog = new JournalCatalog();
            var assemblies = sp.GetRequiredService<JournalFactDiscovery>().Assemblies;
            if (assemblies.Count > 0)
                catalog.RegisterFromAssemblies(assemblies.ToArray());
            return catalog;
        });
        services.TryAddSingleton<IJournalCatalog>(sp => sp.GetRequiredService<JournalCatalog>());

        services.TryAddSingleton<JournalDrainMetrics>();
        services.TryAddSingleton<IJournalHealth, JournalHealth>();
        services.TryAddSingleton<IJournalDrainPolicy, JournalDrainPolicy>();
        services.TryAddSingleton<IJournalQueue, JournalQueue>();
        services.TryAddSingleton<IJournalLiveFeed, JournalLiveFeed>();
        services.TryAddSingleton<IJournalWriter, JournalWriter>();
        services.TryAddSingleton(TimeProvider.System);

        services.TryAddScoped<IJournalRepository, JournalRepository>();
        services.TryAddScoped<IJournalReader, JournalReader>();
        services.TryAddScoped<IJournalAccess, JournalAccessAdapter>();
        services.TryAddScoped<IListJournalEntriesUseCase, ListJournalEntriesHandler>();
        services.TryAddScoped<IJournalDatabaseInitializer, JournalDatabaseInitializer>();
        services.TryAddSingleton<JournalHealthCheck>();

        if (!services.Any(d => d.ServiceType == typeof(JournalWorkerRegistration)))
        {
            services.AddSingleton<JournalWorkerRegistration>();
            services.AddHostedService<JournalWorker>();
        }

        // Marker avoids duplicate health-check registration when AddJournal is called twice.
        if (!services.Any(d => d.ServiceType == typeof(JournalHealthCheckRegistration)))
        {
            services.AddSingleton<JournalHealthCheckRegistration>();
            services.AddHealthChecks()
                .AddCheck<JournalHealthCheck>("journal", tags: ["journal", "ready"]);
        }

        return services;
    }

    /// <summary>
    /// Scans the host assembly for <c>[JournalFact]</c> types and queues them
    /// for catalog registration. Requires <see cref="AddJournal"/> first.
    /// </summary>
    public static IServiceCollection DiscoverJournalFacts(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var discovery = services
            .Select(d => d.ImplementationInstance)
            .OfType<JournalFactDiscovery>()
            .FirstOrDefault();

        if (discovery is null)
        {
            throw new InvalidOperationException(
                "DiscoverJournalFacts requires AddJournal() to be called first.");
        }

        discovery.Add(Assembly.GetExecutingAssembly());
        return services;
    }

    private sealed class JournalHealthCheckRegistration;

    private sealed class JournalWorkerRegistration;
}
