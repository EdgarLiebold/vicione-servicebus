using System;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Clients.Contexts;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Creates request-client factories bound to a bus or connected response endpoint.</summary>
public static class ClientFactoryExtensions
{
    /// <summary>Creates a request client that sends to an explicit destination and receives responses on the bus endpoint.</summary>
    /// <typeparam name="TRequest">The request message contract.</typeparam>
    /// <param name="bus">The bus used to send the request and receive its response.</param>
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
    /// <typeparam name="TRequest">The request message contract.</typeparam>
    /// <param name="bus">The bus used to publish the request and receive its response.</param>
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
    /// <typeparam name="TRequest">The request message contract.</typeparam>
    /// <param name="consumeContext">The consumed message whose correlation metadata is propagated to the request.</param>
    /// <param name="bus">The bus used to send the request and receive its response.</param>
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
    /// <typeparam name="TRequest">The request message contract.</typeparam>
    /// <param name="consumeContext">The consumed message whose correlation metadata is propagated to the request.</param>
    /// <param name="bus">The bus used to publish the request and receive its response.</param>
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
    /// <param name="bus">The bus used to publish or send requests and receive responses.</param>
    /// <param name="timeout">The default request timeout.</param>
    /// <returns>The created client factory.</returns>
    public static IClientFactory CreateClientFactory(this IBus bus, RequestTimeout timeout = default)
    {
        ArgumentNullException.ThrowIfNull(bus);

        return new ClientFactory(new BusClientFactoryContext(bus, timeout));
    }

    /// <summary>Creates a client factory whose response address and lifetime are owned by a connected receive endpoint.</summary>
    /// <param name="receiveEndpointHandle">The response endpoint, which is stopped when the client factory is disposed.</param>
    /// <param name="timeout">The default request timeout.</param>
    /// <returns>The created client factory.</returns>
    public static IClientFactory CreateClientFactory(this IHostReceiveEndpointHandle receiveEndpointHandle, RequestTimeout timeout = default)
    {
        ArgumentNullException.ThrowIfNull(receiveEndpointHandle);

        var context = new HostReceiveEndpointClientFactoryContext(receiveEndpointHandle, timeout);

        return new ClientFactory(context);
    }

    /// <summary>Creates a client factory using the connector's response endpoint.</summary>
    /// <param name="connector">The connector that supplies the response endpoint.</param>
    /// <param name="timeout">The default request timeout.</param>
    /// <returns>The created client factory.</returns>
    public static IClientFactory CreateClientFactory(this IReceiveConnector connector, RequestTimeout timeout = default)
    {
        ArgumentNullException.ThrowIfNull(connector);

        var receiveEndpointHandle = connector.ConnectResponseEndpoint();

        return receiveEndpointHandle.CreateClientFactory(timeout);
    }

    /// <summary>Connects a temporary response endpoint and creates a client factory that owns it.</summary>
    /// <param name="connector">The connector used to create the temporary response endpoint.</param>
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
