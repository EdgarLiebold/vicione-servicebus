using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Creates request clients over a configured client-factory context.</summary>
public sealed class ClientFactory :
    IClientFactory,
    IAsyncDisposable
{
    /// <summary>Creates a request-client factory over the supplied provider context.</summary>
    /// <param name="context">The provider context used for routing, response connections, time, and endpoint resolution.</param>
    public ClientFactory(ClientFactoryContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>Asynchronously releases an owned client-factory context when it has a lifetime.</summary>
    /// <returns>A task that completes when the owned context is released.</returns>
    public ValueTask DisposeAsync()
    {
        if (Context is IAsyncDisposable asyncDisposable)
            return asyncDisposable.DisposeAsync();

        return default;
    }

    /// <summary>Gets the provider context used by request clients created by this factory.</summary>
    public ClientFactoryContext Context { get; }

    /// <summary>Creates a request that uses the configured route or publishes when no route exists.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="message">The request message.</param>
    /// <param name="timeout">The maximum response-wait duration.</param>
    /// <param name="cancellationToken">Cancels sending or response waiting.</param>
    /// <returns>The request handle.</returns>
    public RequestHandle<T> CreateRequest<T>(T message, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        IRequestClient<T> client = CreateRequestClient<T>(timeout);

        return client.Create(message, cancellationToken: cancellationToken);
    }

    /// <summary>Creates a request for an explicit destination.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The request message.</param>
    /// <param name="timeout">The maximum response-wait duration.</param>
    /// <param name="cancellationToken">Cancels sending or response waiting.</param>
    /// <returns>The request handle.</returns>
    public RequestHandle<T> CreateRequest<T>(Uri destinationAddress, T message, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        IRequestClient<T> client = CreateRequestClient<T>(destinationAddress, timeout);

        return client.Create(message, cancellationToken: cancellationToken);
    }

    /// <summary>Creates a request correlated with the current consumed message.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="consumeContext">The consumed message whose correlation metadata is propagated.</param>
    /// <param name="message">The request message.</param>
    /// <param name="timeout">The maximum response-wait duration.</param>
    /// <param name="cancellationToken">Cancels sending or response waiting.</param>
    /// <returns>The request handle.</returns>
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, T message, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        IRequestClient<T> client = CreateRequestClient<T>(consumeContext, timeout);

        return client.Create(message, cancellationToken: cancellationToken);
    }

    /// <summary>Creates a correlated request for an explicit destination.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="consumeContext">The consumed message whose correlation metadata is propagated.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The request message.</param>
    /// <param name="timeout">The maximum response-wait duration.</param>
    /// <param name="cancellationToken">Cancels sending or response waiting.</param>
    /// <returns>The request handle.</returns>
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, Uri destinationAddress, T message, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        IRequestClient<T> client = CreateRequestClient<T>(consumeContext, destinationAddress, timeout);

        return client.Create(message, cancellationToken: cancellationToken);
    }

    /// <summary>Creates and initializes a request that uses the configured route or publish fallback.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="values">The values used to initialize the request.</param>
    /// <param name="timeout">The maximum response-wait duration.</param>
    /// <param name="cancellationToken">Cancels initialization, sending, or response waiting.</param>
    /// <returns>The request handle.</returns>
    public RequestHandle<T> CreateRequest<T>(object values, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
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
        IRequestClient<T> client = CreateRequestClient<T>(destinationAddress, timeout);

        return client.Create(values, cancellationToken: cancellationToken);
    }

    /// <summary>Creates and initializes a request correlated with the current consumed message.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="consumeContext">The consumed message whose correlation metadata is propagated.</param>
    /// <param name="values">The values used to initialize the request.</param>
    /// <param name="timeout">The maximum response-wait duration.</param>
    /// <param name="cancellationToken">Cancels initialization, sending, or response waiting.</param>
    /// <returns>The request handle.</returns>
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, object values, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        IRequestClient<T> client = CreateRequestClient<T>(consumeContext, timeout);

        return client.Create(values, cancellationToken: cancellationToken);
    }

    /// <summary>Creates and initializes a correlated request for an explicit destination.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="consumeContext">The consumed message whose correlation metadata is propagated.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="values">The values used to initialize the request.</param>
    /// <param name="timeout">The maximum response-wait duration.</param>
    /// <param name="cancellationToken">Cancels initialization, sending, or response waiting.</param>
    /// <returns>The request handle.</returns>
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, Uri destinationAddress, object values, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        IRequestClient<T> client = CreateRequestClient<T>(consumeContext, destinationAddress, timeout);

        return client.Create(values, cancellationToken: cancellationToken);
    }

    /// <summary>Creates a request client that uses the configured route or publishes when no route exists.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="timeout">The default response-wait duration.</param>
    /// <returns>The request client.</returns>
    public IRequestClient<T> CreateRequestClient<T>(RequestTimeout timeout)
        where T : class
    {
        if (Context.MessageRoutes.TryGetDestinationAddress<T>(out var destinationAddress))
            return CreateRequestClient<T>(destinationAddress, timeout);

        return new RequestClient<T>(Context, Context.GetRequestEndpoint<T>(), timeout.Or(Context.DefaultTimeout));
    }

    /// <summary>Creates a request client that propagates an optional consume context.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="consumeContext">The consumed message whose correlation metadata is propagated, or <see langword="null" />.</param>
    /// <param name="timeout">The default response-wait duration.</param>
    /// <returns>The request client.</returns>
    public IRequestClient<T> CreateRequestClient<T>(ConsumeContext? consumeContext, RequestTimeout timeout)
        where T : class
    {
        if (Context.MessageRoutes.TryGetDestinationAddress<T>(out var destinationAddress))
            return CreateRequestClient<T>(consumeContext, destinationAddress, timeout);

        return new RequestClient<T>(Context, Context.GetRequestEndpoint<T>(consumeContext), timeout.Or(Context.DefaultTimeout));
    }

    /// <summary>Creates a request client for an explicit destination.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="timeout">The default response-wait duration.</param>
    /// <returns>The request client.</returns>
    public IRequestClient<T> CreateRequestClient<T>(Uri destinationAddress, RequestTimeout timeout)
        where T : class
    {
        IRequestSendEndpoint<T> requestSendEndpoint = Context.GetRequestEndpoint<T>(destinationAddress);

        return new RequestClient<T>(Context, requestSendEndpoint, timeout.Or(Context.DefaultTimeout));
    }

    /// <summary>Creates a correlated request client for an explicit destination.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="consumeContext">The consumed message whose correlation metadata is propagated, or <see langword="null" />.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="timeout">The default response-wait duration.</param>
    /// <returns>The request client.</returns>
    public IRequestClient<T> CreateRequestClient<T>(ConsumeContext? consumeContext, Uri destinationAddress, RequestTimeout timeout)
        where T : class
    {
        return new RequestClient<T>(Context, Context.GetRequestEndpoint<T>(destinationAddress, consumeContext), timeout.Or(Context.DefaultTimeout));
    }
}
