using System;
using ViciOne.ServiceBus.AmazonSqs.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Provides Amazon SQS host and receive-endpoint configuration extensions.</summary>
public static class AmazonSqsHostConfigurationExtensions
{
    /// <summary>Configures an Amazon SQS host from an <c>amazonsqs://</c> address.</summary>
    /// <param name="configurator">The Amazon SQS bus configurator.</param>
    /// <param name="hostAddress">The host address containing the AWS region and optional scope.</param>
    /// <param name="configure">The callback that configures the host.</param>
    public static void Host(this IAmazonSqsBusFactoryConfigurator configurator, Uri hostAddress, Action<IAmazonSqsHostConfigurator> configure)
    {
        if (hostAddress == null)
            throw new ArgumentNullException(nameof(hostAddress));

        var hostConfigurator = new AmazonSqsHostConfigurator(hostAddress);

        configure(hostConfigurator);

        configurator.Host(hostConfigurator.Settings);
    }

    /// <summary>Configures an Amazon SQS host by AWS region system name.</summary>
    /// <param name="configurator">The Amazon SQS bus configurator.</param>
    /// <param name="hostName">The AWS region system name.</param>
    /// <param name="configure">The callback that configures the host.</param>
    public static void Host(this IAmazonSqsBusFactoryConfigurator configurator, string hostName, Action<IAmazonSqsHostConfigurator> configure)
    {
        configurator.Host(new UriBuilder("amazonsqs", hostName).Uri, configure);
    }

    /// <summary>
    /// Configures a temporary receive endpoint with a generated, non-durable, auto-delete Amazon SQS queue.
    /// </summary>
    /// <param name="configurator">The Amazon SQS bus configurator.</param>
    /// <param name="configure">An optional callback that configures the receive endpoint.</param>
    public static void ReceiveEndpoint(this IAmazonSqsBusFactoryConfigurator configurator, Action<IAmazonSqsReceiveEndpointConfigurator>? configure = null)
    {
        configurator.ReceiveEndpoint(new TemporaryEndpointDefinition(), DefaultEndpointNameFormatter.Instance, configure);
    }

    /// <summary>Configures an Amazon SQS receive endpoint from an endpoint definition.</summary>
    /// <param name="configurator">The Amazon SQS bus configurator.</param>
    /// <param name="definition">The endpoint definition.</param>
    /// <param name="configure">An optional callback that configures the receive endpoint.</param>
    public static void ReceiveEndpoint(this IAmazonSqsBusFactoryConfigurator configurator, IEndpointDefinition definition,
        Action<IAmazonSqsReceiveEndpointConfigurator>? configure = null)
    {
        configurator.ReceiveEndpoint(definition, DefaultEndpointNameFormatter.Instance, configure);
    }
}
