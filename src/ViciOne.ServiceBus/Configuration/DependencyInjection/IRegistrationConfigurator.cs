using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures registration.</summary>
public interface IRegistrationConfigurator :
    IRegistrationConfiguratorServices
{
    /// <summary>Adds the consumer, allowing configuration when it is configured on an endpoint.</summary>
    /// <typeparam name="T">The consumer type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The consumer registration configurator produced by the operation.</returns>
    IConsumerRegistrationConfigurator<T> AddConsumer<T>(Action<IRegistrationContext, IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer;

    /// <summary>Adds the consumer, allowing configuration when it is configured on an endpoint.</summary>
    /// <typeparam name="T">The consumer type.</typeparam>
    /// <param name="consumerDefinitionType">The consumer definition type.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The consumer registration configurator produced by the operation.</returns>
    IConsumerRegistrationConfigurator<T> AddConsumer<T>(Type? consumerDefinitionType,
        Action<IRegistrationContext, IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer;

    /// <summary>
    /// Adds an endpoint definition, which will to used for consumers, sagas, etc. that are on that same endpoint. If a consumer, etc.
    /// specifies an endpoint without a definition, the default endpoint definition is used if one cannot be resolved from the configuration
    /// service provider (via generic registration).
    /// </summary>
    /// <param name="endpointDefinition">The endpoint definition to add.</param>
    void AddEndpoint(Type endpointDefinition);

    /// <summary>
    /// Add a request client, for the request type, which uses the <see cref="ConsumeContext" /> if present, otherwise
    /// uses the <see cref="IBus" />. The request is published, unless an endpoint convention is specified for the
    /// request type.
    /// </summary>
    /// <typeparam name="T">The request message type.</typeparam>
    /// <param name="timeout">The request timeout.</param>
    void AddRequestClient<T>(RequestTimeout timeout = default)
        where T : class;

    /// <summary>
    /// Add a request client, for the request type, which uses the <see cref="ConsumeContext" /> if present, otherwise
    /// uses the <see cref="IBus" />.
    /// </summary>
    /// <typeparam name="T">The request message type.</typeparam>
    /// <param name="destinationAddress">The destination address for the request.</param>
    /// <param name="timeout">The request timeout.</param>
    void AddRequestClient<T>(Uri destinationAddress, RequestTimeout timeout = default)
        where T : class;

    /// <summary>
    /// Add a request client, for the request type, which uses the <see cref="ConsumeContext" /> if present, otherwise
    /// uses the <see cref="IBus" />. The request is published, unless an endpoint convention is specified for the
    /// request type.
    /// </summary>
    /// <param name="requestType">The request message type.</param>
    /// <param name="timeout">The request timeout.</param>
    void AddRequestClient(Type requestType, RequestTimeout timeout = default);

    /// <summary>
    /// Add a request client, for the request type, which uses the <see cref="ConsumeContext" /> if present, otherwise
    /// uses the <see cref="IBus" />.
    /// </summary>
    /// <param name="requestType">The request message type.</param>
    /// <param name="destinationAddress">The destination address for the request.</param>
    /// <param name="timeout">The request timeout.</param>
    void AddRequestClient(Type requestType, Uri destinationAddress, RequestTimeout timeout = default);

    /// <summary>Sets the default request timeout for this bus instance, used by the client factory to create request clients.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    void SetDefaultRequestTimeout(RequestTimeout timeout);

    /// <summary>Set the default endpoint name formatter used for endpoint names.</summary>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    void SetEndpointNameFormatter(IEndpointNameFormatter endpointNameFormatter);

}
