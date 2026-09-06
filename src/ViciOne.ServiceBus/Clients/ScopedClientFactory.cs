using System;
using System.Threading;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Creates scoped client instances.</summary>
public class ScopedClientFactory :
    IScopedClientFactory
{
    readonly IClientFactory _clientFactory;
    readonly ConsumeContext? _consumeContext;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="clientFactory">The client factory.</param>
    /// <param name="consumeContext">The consume context.</param>
    public ScopedClientFactory(IClientFactory clientFactory, ConsumeContext? consumeContext)
    {
        _clientFactory = clientFactory;
        _consumeContext = consumeContext;
    }

    /// <summary>Creates request.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    public RequestHandle<T> CreateRequest<T>(T message, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        IRequestClient<T> client = CreateRequestClient<T>(timeout);

        return client.Create(message, cancellationToken: cancellationToken);
    }

    /// <summary>Creates request.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    public RequestHandle<T> CreateRequest<T>(Uri destinationAddress, T message, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        IRequestClient<T> client = CreateRequestClient<T>(destinationAddress, timeout);

        return client.Create(message, cancellationToken: cancellationToken);
    }

    /// <summary>Creates request.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    public RequestHandle<T> CreateRequest<T>(object values, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        IRequestClient<T> client = CreateRequestClient<T>(timeout);

        return client.Create(values, cancellationToken: cancellationToken);
    }

    /// <summary>Creates request.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="values">The values.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    public RequestHandle<T> CreateRequest<T>(Uri destinationAddress, object values, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        IRequestClient<T> client = CreateRequestClient<T>(destinationAddress, timeout);

        return client.Create(values, cancellationToken: cancellationToken);
    }

    /// <summary>Creates request client.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <returns>The created request client.</returns>
    public IRequestClient<T> CreateRequestClient<T>(RequestTimeout timeout)
        where T : class
    {
        if (_clientFactory.Context.MessageRoutes.TryGetDestinationAddress<T>(out var destinationAddress))
            return CreateRequestClient<T>(destinationAddress, timeout);

        return _clientFactory.CreateRequestClient<T>(_consumeContext, timeout);
    }

    /// <summary>Creates request client.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <returns>The created request client.</returns>
    public IRequestClient<T> CreateRequestClient<T>(Uri destinationAddress, RequestTimeout timeout)
        where T : class
    {
        return _clientFactory.CreateRequestClient<T>(_consumeContext, destinationAddress, timeout);
    }
}
