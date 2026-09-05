using System;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.AmazonSqs;
using ViciOne.ServiceBus.AmazonSqs.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for amazon sqs bus factory configurator.
/// </summary>
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
        configurator.Services.AddOptions<AmazonSqsTransportOptions>(string.Empty)
            .Validate(
                static options => options.Region is null || !string.IsNullOrWhiteSpace(options.Region),
                "Amazon SQS transport for bus 'default': Region must not be empty when specified. Set an AWS region system name or leave it unset.")
            .Validate(
                static options => options.Scope is null || !string.IsNullOrWhiteSpace(options.Scope),
                "Amazon SQS transport for bus 'default': Scope must not be empty when specified. Set a non-empty scope or leave it unset.")
            .Validate(
                static options => string.IsNullOrWhiteSpace(options.Scope) || !string.IsNullOrWhiteSpace(options.Region),
                "Amazon SQS transport for bus 'default': Scope requires Region. Set Region whenever a scope is configured.")
            .ValidateOnStart();
        configurator.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, AmazonSqsSendFailureClassifier>());
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
