using System;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures Azure Service Bus hosts and receive endpoints on a bus factory.</summary>
public static class ServiceBusBusFactoryConfiguratorExtensions
{
    /// <summary>Configures a namespace from an absolute Azure Service Bus host address.</summary>
    /// <param name="configurator">The bus factory to configure.</param>
    /// <param name="hostAddress">The namespace address, optionally followed by an entity-path scope.</param>
    /// <param name="configure">Optionally configures credentials, transport, and retry settings.</param>
    public static void Host(this IServiceBusBusFactoryConfigurator configurator, Uri hostAddress,
        Action<IServiceBusHostConfigurator>? configure = null)
    {
        var hostConfigurator = new ServiceBusHostConfigurator(hostAddress);

        configure?.Invoke(hostConfigurator);

        configurator.Host(hostConfigurator.Settings);
    }

    /// <summary>Configures a namespace backed by caller-owned Azure SDK clients.</summary>
    /// <param name="configurator">The bus factory to configure.</param>
    /// <param name="hostAddress">The namespace address, optionally followed by an entity-path scope.</param>
    /// <param name="serviceBusClient">The client used for message operations.</param>
    /// <param name="serviceBusAdministrationClient">The client used for namespace administration.</param>
    public static void Host(this IServiceBusBusFactoryConfigurator configurator, Uri hostAddress,
        ServiceBusClient serviceBusClient, ServiceBusAdministrationClient serviceBusAdministrationClient)
    {
        var hostConfigurator = new ServiceBusHostConfigurator(hostAddress, serviceBusClient, serviceBusAdministrationClient);

        configurator.Host(hostConfigurator.Settings);
    }

    /// <summary>Configures a namespace from an Azure connection string or absolute endpoint address.</summary>
    /// <param name="configurator">The bus factory to configure.</param>
    /// <param name="connectionString">An Azure Service Bus connection string or absolute namespace address.</param>
    /// <param name="configure">Optionally configures credentials, transport, and retry settings.</param>
    public static void Host(this IServiceBusBusFactoryConfigurator configurator, string connectionString,
        Action<IServiceBusHostConfigurator>? configure = null)
    {
        // Accept both Azure connection strings and absolute endpoint addresses.
        if (Uri.IsWellFormedUriString(connectionString, UriKind.Absolute))
        {
            var hostAddress = new Uri(connectionString);

            Host(configurator, hostAddress, configure);
        }
        else
        {
            var hostConfigurator = new ServiceBusHostConfigurator(connectionString);

            configure?.Invoke(hostConfigurator);

            configurator.Host(hostConfigurator.Settings);
        }
    }

    /// <summary>Configures authentication with a precomputed shared access signature.</summary>
    /// <param name="configurator">The host configuration to update.</param>
    /// <param name="configure">Supplies the signature and its validity interval.</param>
    public static void SharedAccessSignature(this IServiceBusHostConfigurator configurator,
        Action<ISharedAccessSignatureTokenProviderConfigurator> configure)
    {
        var tokenProviderConfigurator = new SharedAccessSignatureTokenProviderConfigurator();

        configure(tokenProviderConfigurator);

        configurator.SasCredential = tokenProviderConfigurator.SasCredential;
    }

    /// <summary>Configures authentication with an Azure shared access key name and value.</summary>
    /// <param name="configurator">The host configuration to update.</param>
    /// <param name="configure">Supplies the shared access key credentials.</param>
    public static void NamedKey(this IServiceBusHostConfigurator configurator,
        Action<IServiceBusNamedKeyTokenProviderConfigurator> configure)
    {
        var namedKeyConfigurator = new NamedKeyTokenProviderConfigurator();

        configure(namedKeyConfigurator);

        configurator.NamedKeyCredential = namedKeyConfigurator.NamedKeyCredential;
    }

    /// <summary>
    /// Registers a temporary receive endpoint with a generated queue name.
    /// The queue is non-durable and automatically deleted after its configured idle interval.
    /// </summary>
    /// <param name="configurator">The bus factory to configure.</param>
    /// <param name="configure">Optionally configures the temporary endpoint.</param>
    public static void ReceiveEndpoint(this IServiceBusBusFactoryConfigurator configurator, Action<IServiceBusReceiveEndpointConfigurator>? configure = null)
    {
        configurator.ReceiveEndpoint(new TemporaryEndpointDefinition(), DefaultEndpointNameFormatter.Instance, configure);
    }

    /// <summary>Registers a receive endpoint from an endpoint definition.</summary>
    /// <param name="configurator">The bus factory to configure.</param>
    /// <param name="definition">The definition that supplies the endpoint name and common settings.</param>
    /// <param name="configure">Optionally configures Azure Service Bus-specific endpoint settings.</param>
    public static void ReceiveEndpoint(this IServiceBusBusFactoryConfigurator configurator, IEndpointDefinition definition,
        Action<IServiceBusReceiveEndpointConfigurator>? configure = null)
    {
        configurator.ReceiveEndpoint(definition, DefaultEndpointNameFormatter.Instance, configure);
    }
}
