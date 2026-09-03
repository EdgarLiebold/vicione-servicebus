using System;
using System.Linq;
using ViciOne.ServiceBus;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Composition helpers for the application-wide immutable message-contract catalog.</summary>
public static class MessageContractServiceCollectionExtensions
{
    /// <summary>
    /// Builds and registers the one immutable stable message-contract catalog used by durable infrastructure.
    /// Re-registering the catalog is rejected instead of making registration order semantic.
    /// </summary>
    public static IServiceCollection AddViciOneMessageContracts(
        this IServiceCollection services,
        Action<MessageContractCatalogBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        if (services.Any(static descriptor => descriptor.ServiceType == typeof(IMessageContractCatalog)))
        {
            throw new ConfigurationException(
                "The ViciOne message-contract catalog is already registered. Exactly one immutable application-wide catalog is allowed.");
        }

        var builder = new MessageContractCatalogBuilder();
        configure(builder);
        IMessageContractCatalog catalog = builder.Build();
        services.AddSingleton(catalog);
        return services;
    }
}
