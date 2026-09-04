using System;
using System.Threading;

namespace ViciOne.ServiceBus.Clients;

/// <summary>
/// Provides a scoped client factory implementation.
/// </summary>
public class ScopedClientFactory :
    IScopedClientFactory
{
    readonly IClientFactory _clientFactory;
    readonly ConsumeContext? _consumeContext;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="clientFactory">The client factory value.</param>
    /// <param name="consumeContext">The consume context value.</param>
    public ScopedClientFactory(IClientFactory clientFactory, ConsumeContext? consumeContext)
    {
        _clientFactory = clientFactory;
        _consumeContext = consumeContext;
    }

    /// <summary>
    /// Creates request.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <returns>The result of the operation.</returns>
    public RequestHandle<T> CreateRequest<T>(T message, CancellationToken cancellationToken, RequestTimeout timeout)
        where T : class
    {
        IRequestClient<T> client = CreateRequestClient<T>(timeout);

        return client.Create(message, cancellationToken);
    }

    /// <summary>
    /// Creates request.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <returns>The result of the operation.</returns>
    public RequestHandle<T> CreateRequest<T>(Uri destinationAddress, T message, CancellationToken cancellationToken, RequestTimeout timeout)
        where T : class
    {
        IRequestClient<T> client = CreateRequestClient<T>(destinationAddress, timeout);

        return client.Create(message, cancellationToken);
    }

    /// <summary>
    /// Creates request.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <returns>The result of the operation.</returns>
    public RequestHandle<T> CreateRequest<T>(object values, CancellationToken cancellationToken, RequestTimeout timeout)
        where T : class
    {
        IRequestClient<T> client = CreateRequestClient<T>(timeout);

        return client.Create(values, cancellationToken);
    }

    /// <summary>
    /// Creates request.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="values">The values value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <returns>The result of the operation.</returns>
    public RequestHandle<T> CreateRequest<T>(Uri destinationAddress, object values, CancellationToken cancellationToken, RequestTimeout timeout)
        where T : class
    {
        IRequestClient<T> client = CreateRequestClient<T>(destinationAddress, timeout);

        return client.Create(values, cancellationToken);
    }

    /// <summary>
    /// Creates request client.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="timeout">The timeout value.</param>
    /// <returns>The result of the operation.</returns>
    public IRequestClient<T> CreateRequestClient<T>(RequestTimeout timeout)
        where T : class
    {
        if (_clientFactory.Context.MessageRoutes.TryGetDestinationAddress<T>(out var destinationAddress))
            return CreateRequestClient<T>(destinationAddress, timeout);

        return _clientFactory.CreateRequestClient<T>(_consumeContext, timeout);
    }

    /// <summary>
    /// Creates request client.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <returns>The result of the operation.</returns>
    public IRequestClient<T> CreateRequestClient<T>(Uri destinationAddress, RequestTimeout timeout)
        where T : class
    {
        return _clientFactory.CreateRequestClient<T>(_consumeContext, destinationAddress, timeout);
    }
}
