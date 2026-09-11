using System;
using System.Threading;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Creates request clients that preserve the current consume scope when one exists.</summary>
public sealed class ScopedClientFactory :
    IScopedClientFactory
{
    readonly IClientFactory _clientFactory;
    readonly ConsumeContext? _consumeContext;

    /// <summary>Creates a scoped facade over an existing client factory.</summary>
    /// <param name="clientFactory">The factory that owns request-client infrastructure.</param>
    /// <param name="consumeContext">The current consume context, or <see langword="null" /> outside consumption.</param>
    public ScopedClientFactory(IClientFactory clientFactory, ConsumeContext? consumeContext)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _consumeContext = consumeContext;
    }

    /// <summary>Creates a request that uses the configured route or publishes when no route exists.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="message">The request message.</param>
    /// <param name="timeout">The maximum response-wait duration.</param>
    /// <param name="cancellationToken">Cancels sending or response waiting.</param>
    /// <returns>The request handle.</returns>
    public RequestHandle<T> CreateRequest<T>(T message, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        IRequestClient<T> client = CreateRequestClient<T>(timeout);

        return client.Create(message, cancellationToken: cancellationToken);
    }

    /// <summary>Creates a request for an explicit destination.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="destinationAddress">The request service address.</param>
    /// <param name="message">The request message.</param>
    /// <param name="timeout">The maximum response-wait duration.</param>
    /// <param name="cancellationToken">Cancels sending or response waiting.</param>
    /// <returns>The request handle.</returns>
    public RequestHandle<T> CreateRequest<T>(Uri destinationAddress, T message, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(message);
        IRequestClient<T> client = CreateRequestClient<T>(destinationAddress, timeout);

        return client.Create(message, cancellationToken: cancellationToken);
    }

    /// <summary>Creates and initializes a request that uses the configured route or publishes when no route exists.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="values">The values used to initialize the request.</param>
    /// <param name="timeout">The maximum response-wait duration.</param>
    /// <param name="cancellationToken">Cancels initialization, sending, or response waiting.</param>
    /// <returns>The request handle.</returns>
    public RequestHandle<T> CreateRequest<T>(object values, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        IRequestClient<T> client = CreateRequestClient<T>(timeout);

        return client.Create(values, cancellationToken: cancellationToken);
    }

    /// <summary>Creates and initializes a request for an explicit destination.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="values">The values used to initialize the request.</param>
    /// <param name="timeout">The maximum response-wait duration.</param>
    /// <param name="cancellationToken">Cancels initialization, sending, or response waiting.</param>
    /// <returns>The request handle.</returns>
    public RequestHandle<T> CreateRequest<T>(Uri destinationAddress, object values, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(values);
        IRequestClient<T> client = CreateRequestClient<T>(destinationAddress, timeout);

        return client.Create(values, cancellationToken: cancellationToken);
    }

    /// <summary>Creates a request client that uses the configured route or publishes when no route exists.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="timeout">The default response-wait duration.</param>
    /// <returns>The request client.</returns>
    public IRequestClient<T> CreateRequestClient<T>(RequestTimeout timeout)
        where T : class
    {
        if (_clientFactory.Context.MessageRoutes.TryGetDestinationAddress<T>(out var destinationAddress))
            return CreateRequestClient<T>(destinationAddress, timeout);

        return _consumeContext is null
            ? _clientFactory.CreateRequestClient<T>(timeout)
            : _clientFactory.CreateRequestClient<T>(_consumeContext, timeout);
    }

    /// <summary>Creates a request client for an explicit destination.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="destinationAddress">The request service address.</param>
    /// <param name="timeout">The default response-wait duration.</param>
    /// <returns>The request client.</returns>
    public IRequestClient<T> CreateRequestClient<T>(Uri destinationAddress, RequestTimeout timeout)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);

        return _consumeContext is null
            ? _clientFactory.CreateRequestClient<T>(destinationAddress, timeout)
            : _clientFactory.CreateRequestClient<T>(_consumeContext, destinationAddress, timeout);
    }
}
