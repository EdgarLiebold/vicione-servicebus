namespace ViciOne.ServiceBus;

using System;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using AmazonSqsTransport.Configuration;


public static class AmazonSqsBusFactoryConfiguratorExtensions
{
    /// <summary>
    /// Select AmazonSQS as the transport for the service bus
    /// </summary>
    public static IBusControl CreateUsingAmazonSqs(this IBusFactorySelector selector, Action<IAmazonSqsBusFactoryConfigurator> configure)
    {
        return AmazonSqsBusFactory.Create(configure);
    }

    /// <summary>
    /// Configure ViciOne.ServiceBus to use Amazon SQS for the transport.
    /// </summary>
    /// <param name="configurator">The registration configurator (configured via AddViciOneServiceBus)</param>
    /// <param name="configure">The configuration callback for the bus factory</param>
    public static void UsingAmazonSqs(this IBusRegistrationConfigurator configurator,
        Action<IBusRegistrationContext, IAmazonSqsBusFactoryConfigurator>? configure = null)
    {
        configurator.SetBusFactory(new AmazonSqsRegistrationBusFactory(configure));
    }

    /// <summary>
    /// Configure the default Amazon SQS Host, using the FallbackRegionFactory and FallbackCredentialsFactory
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="configure"></param>
    public static void UseDefaultHost(this IAmazonSqsBusFactoryConfigurator configurator, Action<IAmazonSqsHostConfigurator>? configure = null)
    {
        configurator.UseDefaultHost(FallbackRegionFactory.GetRegionEndpoint(), configure);
    }

    /// <summary>
    /// Configure the default Amazon SQS Host, using the FallbackCredentialsFactory with the specified region
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="endpoint">The region for the host</param>
    /// <param name="configure"></param>
    public static void UseDefaultHost(this IAmazonSqsBusFactoryConfigurator configurator, RegionEndpoint endpoint,
        Action<IAmazonSqsHostConfigurator>? configure = null)
    {
        configurator.Host(endpoint.SystemName, h => configure?.Invoke(h));
    }
}
