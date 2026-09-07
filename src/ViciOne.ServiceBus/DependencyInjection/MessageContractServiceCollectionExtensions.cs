using System;
using System.Linq;
using ViciOne.ServiceBus;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Composition helpers for the application-wide immutable message-contract catalog.</summary>
public static class MessageContractServiceCollectionExtensions
{
    /// <summary>
    /// Adds stable contract declarations to the one immutable catalog. Separate bus/feature configuration blocks may
    /// contribute declarations; the catalog is built exactly once when the container materializes it.
    /// </summary>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="configure">The bootstrap callback that contributes stable message contract identities.</param>
    /// <returns>The same service collection for fluent composition.</returns>
    public static IServiceCollection AddViciOneMessageContracts(
        this IServiceCollection services,
        Action<MessageContractCatalogBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        bool owned = services.Any(static descriptor =>
            descriptor.ServiceType == typeof(MessageContractCatalogRegistrationOwner));
        if (!owned && services.Any(static descriptor => descriptor.ServiceType == typeof(IMessageContractCatalog)))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Message contract catalog",
                    "all configured buses",
                    $"An {nameof(IMessageContractCatalog)} is already registered outside {nameof(AddViciOneMessageContracts)}",
                    "Use one catalog ownership model so all declarations compose into a single immutable catalog"));
        }

        if (!owned)
        {
            services.AddSingleton<MessageContractCatalogRegistrationOwner>();
            services.AddSingleton<IMessageContractCatalog>(provider =>
            {
                var builder = new MessageContractCatalogBuilder();
                foreach (MessageContractCatalogRegistration registration in
                         provider.GetServices<MessageContractCatalogRegistration>())
                    registration.Configure(builder);

                return builder.Build();
            });
        }

        services.AddSingleton(new MessageContractCatalogRegistration(configure));
        return services;
    }

    private sealed class MessageContractCatalogRegistrationOwner;

    private sealed class MessageContractCatalogRegistration(
        Action<MessageContractCatalogBuilder> configure)
    {
        public Action<MessageContractCatalogBuilder> Configure { get; } = configure;
    }
}
