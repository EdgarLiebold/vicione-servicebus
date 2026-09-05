using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for registration configurator.
/// </summary>
public interface IRegistrationConfigurator :
    IRegistrationConfiguratorServices
{
    /// <summary>
    /// Adds the consumer, allowing configuration when it is configured on an endpoint
    /// </summary>
    /// <param name="configure"></param>
    /// <typeparam name="T">The consumer type</typeparam>
    IConsumerRegistrationConfigurator<T> AddConsumer<T>(Action<IRegistrationContext, IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer;

    /// <summary>
    /// Adds the consumer, allowing configuration when it is configured on an endpoint
    /// </summary>
    /// <param name="consumerDefinitionType">The consumer definition type</param>
    /// <param name="configure"></param>
    /// <typeparam name="T">The consumer type</typeparam>
    IConsumerRegistrationConfigurator<T> AddConsumer<T>(Type? consumerDefinitionType,
        Action<IRegistrationContext, IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer;

    /// <summary>
    /// Adds an endpoint definition, which will to used for consumers, sagas, etc. that are on that same endpoint. If a consumer, etc.
    /// specifies an endpoint without a definition, the default endpoint definition is used if one cannot be resolved from the configuration
    /// service provider (via generic registration).
    /// </summary>
    /// <param name="endpointDefinition">The endpoint definition to add</param>
    void AddEndpoint(Type endpointDefinition);

    /// <summary>
    /// Add a request client, for the request type, which uses the <see cref="ConsumeContext" /> if present, otherwise
    /// uses the <see cref="IBus" />. The request is published, unless an endpoint convention is specified for the
    /// request type.
    /// </summary>
    /// <param name="timeout">The request timeout</param>
    /// <typeparam name="T">The request message type</typeparam>
    void AddRequestClient<T>(RequestTimeout timeout = default)
        where T : class;

    /// <summary>
    /// Add a request client, for the request type, which uses the <see cref="ConsumeContext" /> if present, otherwise
    /// uses the <see cref="IBus" />.
    /// </summary>
    /// <param name="destinationAddress">The destination address for the request</param>
    /// <param name="timeout">The request timeout</param>
    /// <typeparam name="T">The request message type</typeparam>
    void AddRequestClient<T>(Uri destinationAddress, RequestTimeout timeout = default)
        where T : class;

    /// <summary>
    /// Add a request client, for the request type, which uses the <see cref="ConsumeContext" /> if present, otherwise
    /// uses the <see cref="IBus" />. The request is published, unless an endpoint convention is specified for the
    /// request type.
    /// </summary>
    /// <param name="requestType">The request message type</param>
    /// <param name="timeout">The request timeout</param>
    void AddRequestClient(Type requestType, RequestTimeout timeout = default);

    /// <summary>
    /// Add a request client, for the request type, which uses the <see cref="ConsumeContext" /> if present, otherwise
    /// uses the <see cref="IBus" />.
    /// </summary>
    /// <param name="requestType">The request message type</param>
    /// <param name="destinationAddress">The destination address for the request</param>
    /// <param name="timeout">The request timeout</param>
    void AddRequestClient(Type requestType, Uri destinationAddress, RequestTimeout timeout = default);

    /// <summary>
    /// Sets the default request timeout for this bus instance, used by the client factory to create request clients
    /// </summary>
    /// <param name="timeout"></param>
    void SetDefaultRequestTimeout(RequestTimeout timeout);

    /// <summary>
    /// Set the default endpoint name formatter used for endpoint names
    /// </summary>
    /// <param name="endpointNameFormatter"></param>
    void SetEndpointNameFormatter(IEndpointNameFormatter endpointNameFormatter);

}

/// <summary>
/// Exposes the underlying dependency-injection collection to advanced registration extensions.
/// Application configuration should use the typed registration methods instead.
/// </summary>
public interface IRegistrationConfiguratorServices
{
    /// <summary>
    /// Gets the dependency-injection collection owned by this configurator.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets the bus contract owned by this registration configurator.
    /// </summary>
    Type BusType { get; }
}
