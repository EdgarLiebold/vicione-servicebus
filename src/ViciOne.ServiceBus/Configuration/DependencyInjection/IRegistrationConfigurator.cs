using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers endpoint components, request clients, and naming defaults for one bus owner.</summary>
public interface IRegistrationConfigurator :
    IRegistrationConfiguratorServices
{
    /// <summary>Registers a consumer with an optional endpoint-time callback.</summary>
    /// <typeparam name="T">The consumer implementation.</typeparam>
    /// <param name="configure">The callback that receives the active registration context and consumer configurator.</param>
    /// <returns>A fluent configurator for the consumer registration.</returns>
    IConsumerRegistrationConfigurator<T> AddConsumer<T>(Action<IRegistrationContext, IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer;

    /// <summary>Registers a consumer with an optional runtime definition and endpoint-time callback.</summary>
    /// <typeparam name="T">The consumer implementation.</typeparam>
    /// <param name="consumerDefinitionType">The concrete consumer definition, or <see langword="null" /> to use convention defaults.</param>
    /// <param name="configure">The callback that receives the active registration context and consumer configurator.</param>
    /// <returns>A fluent configurator for the consumer registration.</returns>
    IConsumerRegistrationConfigurator<T> AddConsumer<T>(Type? consumerDefinitionType,
        Action<IRegistrationContext, IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer;

    /// <summary>
    /// Registers a runtime-selected endpoint definition for its associated consumer, saga, activity, or future.
    /// </summary>
    /// <param name="endpointDefinitionType">The concrete endpoint definition.</param>
    void AddEndpoint(Type endpointDefinitionType);

    /// <summary>
    /// Registers a request client whose destination is resolved from message topology.
    /// </summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="timeout">An explicit timeout, or an unspecified value to inherit the configured default.</param>
    void AddRequestClient<T>(RequestTimeout timeout = default)
        where T : class;

    /// <summary>
    /// Registers a request client bound to an explicit destination.
    /// </summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="destinationAddress">The address to which requests are sent.</param>
    /// <param name="timeout">An explicit timeout, or an unspecified value to inherit the configured default.</param>
    void AddRequestClient<T>(Uri destinationAddress, RequestTimeout timeout = default)
        where T : class;

    /// <summary>
    /// Registers a runtime-selected request client whose destination is resolved from message topology.
    /// </summary>
    /// <param name="requestType">The request message contract.</param>
    /// <param name="timeout">An explicit timeout, or an unspecified value to inherit the configured default.</param>
    void AddRequestClient(Type requestType, RequestTimeout timeout = default);

    /// <summary>
    /// Registers a runtime-selected request client bound to an explicit destination.
    /// </summary>
    /// <param name="requestType">The request message contract.</param>
    /// <param name="destinationAddress">The address to which requests are sent.</param>
    /// <param name="timeout">An explicit timeout, or an unspecified value to inherit the configured default.</param>
    void AddRequestClient(Type requestType, Uri destinationAddress, RequestTimeout timeout = default);

    /// <summary>Sets the timeout inherited by request clients that omit an explicit value.</summary>
    /// <param name="timeout">The default request timeout.</param>
    void SetDefaultRequestTimeout(RequestTimeout timeout);

    /// <summary>Sets the endpoint naming convention for this registration owner.</summary>
    /// <param name="endpointNameFormatter">The naming convention to register.</param>
    void SetEndpointNameFormatter(IEndpointNameFormatter endpointNameFormatter);
}
