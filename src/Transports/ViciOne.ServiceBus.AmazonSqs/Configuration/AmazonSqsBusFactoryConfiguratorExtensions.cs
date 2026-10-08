using System;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.AmazonSqs;
using ViciOne.ServiceBus.AmazonSqs.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides transport-selection and default-host extensions for Amazon SQS.</summary>
public static class AmazonSqsBusFactoryConfiguratorExtensions
{
    /// <summary>Creates a bus that uses Amazon SQS queues and Amazon SNS topics.</summary>
    /// <param name="selector">The bus-factory transport selector.</param>
    /// <param name="configure">The callback that configures the Amazon SQS bus.</param>
    /// <returns>The configured bus control.</returns>
    public static IBusControl CreateUsingAmazonSqs(this IBusFactorySelector selector, Action<IAmazonSqsBusFactoryConfigurator> configure)
    {
        return AmazonSqsBusFactory.Create(configure);
    }

    /// <summary>Registers Amazon SQS as the transport for a dependency-injection bus.</summary>
    /// <param name="configurator">The bus registration configurator.</param>
    /// <param name="configure">An optional callback that configures the bus factory.</param>
    public static void UsingAmazonSqs(this IBusRegistrationConfigurator configurator,
        Action<IBusRegistrationContext, IAmazonSqsBusFactoryConfigurator>? configure = null)
    {
        AddSharedServices(configurator.Services, string.Empty, "default");
        configurator.SetBusFactory(new AmazonSqsRegistrationBusFactory(configure));
    }

    /// <summary>Registers Amazon SQS for a typed bus and validates its named transport options before host startup.</summary>
    /// <typeparam name="TBus">The typed bus contract that owns the named transport options.</typeparam>
    /// <param name="configurator">The typed bus registration configurator.</param>
    /// <param name="configure">An optional callback that configures the bus factory.</param>
    public static void UsingAmazonSqs<TBus>(this IBusRegistrationConfigurator<TBus> configurator,
        Action<IBusRegistrationContext, IAmazonSqsBusFactoryConfigurator>? configure = null)
        where TBus : class, IBus
    {
        AddSharedServices(configurator.Services, typeof(TBus).Name, typeof(TBus).FullName ?? typeof(TBus).Name);
        configurator.SetBusFactory(new AmazonSqsRegistrationBusFactory(configure));
    }

    static void AddSharedServices(IServiceCollection services, string optionsName, string bus)
    {
        services.AddOptions<AmazonSqsTransportOptions>(optionsName)
            .Validate(
                static options => options.Region is null || !string.IsNullOrWhiteSpace(options.Region),
                $"Amazon SQS transport for bus '{bus}': Region must not be empty when specified. Set an AWS region system name or leave it unset.")
            .Validate(
                static options => options.Scope is null || !string.IsNullOrWhiteSpace(options.Scope),
                $"Amazon SQS transport for bus '{bus}': Scope must not be empty when specified. Set a non-empty scope or leave it unset.")
            .Validate(
                static options => string.IsNullOrWhiteSpace(options.Scope) || !string.IsNullOrWhiteSpace(options.Region),
                $"Amazon SQS transport for bus '{bus}': Scope requires Region. Set Region whenever a scope is configured.")
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, AmazonSqsSendFailureClassifier>());
    }

    /// <summary>Configures the default host using the AWS SDK fallback region and credential resolution.</summary>
    /// <param name="configurator">The Amazon SQS bus configurator.</param>
    /// <param name="configure">An optional callback that further configures the host.</param>
    public static void UseDefaultHost(this IAmazonSqsBusFactoryConfigurator configurator, Action<IAmazonSqsHostConfigurator>? configure = null)
    {
        configurator.UseDefaultHost(FallbackRegionFactory.GetRegionEndpoint(), configure);
    }

    /// <summary>Configures the default host for a region using the AWS SDK credential chain.</summary>
    /// <param name="configurator">The Amazon SQS bus configurator.</param>
    /// <param name="endpoint">The AWS region endpoint.</param>
    /// <param name="configure">An optional callback that further configures the host.</param>
    public static void UseDefaultHost(this IAmazonSqsBusFactoryConfigurator configurator, RegionEndpoint endpoint,
        Action<IAmazonSqsHostConfigurator>? configure = null)
    {
        configurator.Host(endpoint.SystemName, h => configure?.Invoke(h));
    }
}
