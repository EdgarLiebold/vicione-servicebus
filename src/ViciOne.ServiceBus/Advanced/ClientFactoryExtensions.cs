using System;
using ViciOne.ServiceBus.Clients;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Creates request-client factories bound to a bus or connected response endpoint.</summary>
public static class ClientFactoryExtensions
{
    /// <summary>Creates a request client that sends to an explicit destination and receives responses on the bus endpoint.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="bus">The bus instance.</param>
    /// <param name="destinationAddress">The request service address.</param>
    /// <param name="timeout">The default request timeout.</param>
    /// <returns>The created request client.</returns>
    public static IRequestClient<TRequest> CreateRequestClient<TRequest>(this IBus bus, Uri destinationAddress, RequestTimeout timeout = default)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(destinationAddress);

        var clientFactory = new ClientFactory(new BusClientFactoryContext(bus, timeout));

        return clientFactory.CreateRequestClient<TRequest>(destinationAddress, timeout);
    }

    /// <summary>Creates a request client that publishes requests and receives responses on the bus endpoint.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="bus">The bus instance.</param>
    /// <param name="timeout">The default request timeout.</param>
    /// <returns>The created request client.</returns>
    public static IRequestClient<TRequest> CreateRequestClient<TRequest>(this IBus bus, RequestTimeout timeout = default)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(bus);

        var clientFactory = new ClientFactory(new BusClientFactoryContext(bus, timeout));

        return clientFactory.CreateRequestClient<TRequest>(timeout);
    }

    /// <summary>Creates a request client that sends to an explicit destination within the current consume scope.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="bus">The bus instance.</param>
    /// <param name="destinationAddress">The request service address.</param>
    /// <param name="timeout">The default request timeout.</param>
    /// <returns>The created request client.</returns>
    public static IRequestClient<TRequest> CreateRequestClient<TRequest>(this ConsumeContext consumeContext, IBus bus, Uri destinationAddress,
        RequestTimeout timeout = default)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(destinationAddress);

        var clientFactory = new ClientFactory(new BusClientFactoryContext(bus, timeout));

        return clientFactory.CreateRequestClient<TRequest>(consumeContext, destinationAddress, timeout);
    }

    /// <summary>Creates a request client that publishes requests within the current consume scope.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="bus">The bus instance.</param>
    /// <param name="timeout">The default request timeout.</param>
    /// <returns>The created request client.</returns>
    public static IRequestClient<TRequest> CreateRequestClient<TRequest>(this ConsumeContext consumeContext, IBus bus, RequestTimeout timeout = default)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        ArgumentNullException.ThrowIfNull(bus);

        var clientFactory = new ClientFactory(new BusClientFactoryContext(bus, timeout));

        return clientFactory.CreateRequestClient<TRequest>(consumeContext, timeout);
    }

    /// <summary>Creates a client factory that receives responses on the bus endpoint.</summary>
    /// <param name="bus">The bus instance.</param>
    /// <param name="timeout">The default request timeout.</param>
    /// <returns>The created client factory.</returns>
    public static IClientFactory CreateClientFactory(this IBus bus, RequestTimeout timeout = default)
    {
        ArgumentNullException.ThrowIfNull(bus);

        return new ClientFactory(new BusClientFactoryContext(bus, timeout));
    }

    /// <summary>Connects a client factory to a host receive endpoint, using the bus as the send endpoint provider.</summary>
    /// <param name="receiveEndpointHandle">A handle to the receive endpoint, which is stopped when the client factory is disposed.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <returns>The created client factory.</returns>
    public static IClientFactory CreateClientFactory(this HostReceiveEndpointHandle receiveEndpointHandle, RequestTimeout timeout = default)
    {
        ArgumentNullException.ThrowIfNull(receiveEndpointHandle);

        var context = new HostReceiveEndpointClientFactoryContext(receiveEndpointHandle, timeout);

        return new ClientFactory(context);
    }

    /// <summary>Connects a new receive endpoint to the host, and creates a <see cref="IClientFactory" />.</summary>
    /// <param name="connector">The host to connect the new receive endpoint.</param>
    /// <param name="timeout">The default request timeout.</param>
    /// <returns>The created client factory.</returns>
    public static IClientFactory CreateClientFactory(this IReceiveConnector connector, RequestTimeout timeout = default)
    {
        ArgumentNullException.ThrowIfNull(connector);

        var receiveEndpointHandle = connector.ConnectResponseEndpoint();

        return receiveEndpointHandle.CreateClientFactory(timeout);
    }

    /// <summary>Connects a new receive endpoint to the host, and creates a <see cref="IClientFactory" />.</summary>
    /// <param name="connector">The host to connect the new receive endpoint.</param>
    /// <param name="timeout">The default request timeout.</param>
    /// <returns>The client factory that owns the connected response endpoint.</returns>
    public static IClientFactory ConnectClientFactory(this IReceiveConnector connector, RequestTimeout timeout = default)
    {
        ArgumentNullException.ThrowIfNull(connector);

        var endpointDefinition = new TemporaryEndpointDefinition();

        var receiveEndpointHandle = connector.ConnectReceiveEndpoint(endpointDefinition, KebabCaseEndpointNameFormatter.Instance);

        return receiveEndpointHandle.CreateClientFactory(timeout);
    }
}
