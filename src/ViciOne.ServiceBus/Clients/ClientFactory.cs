using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Creates request clients over a configured client-factory context.</summary>
public sealed class ClientFactory :
    IClientFactory,
    IAsyncDisposable
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public ClientFactory(ClientFactoryContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask DisposeAsync()
    {
        if (Context is IAsyncDisposable asyncDisposable)
            return asyncDisposable.DisposeAsync();

        return default;
    }

    /// <summary>Gets the context.</summary>
    public ClientFactoryContext Context { get; }

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
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, T message, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        IRequestClient<T> client = CreateRequestClient<T>(consumeContext, timeout);

        return client.Create(message, cancellationToken: cancellationToken);
    }

    /// <summary>Creates request.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, Uri destinationAddress, T message, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        IRequestClient<T> client = CreateRequestClient<T>(consumeContext, destinationAddress, timeout);

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

    /// <summary>Creates request.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="values">The values.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, object values, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        IRequestClient<T> client = CreateRequestClient<T>(consumeContext, timeout);

        return client.Create(values, cancellationToken: cancellationToken);
    }

    /// <summary>Creates request.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="values">The values.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, Uri destinationAddress, object values, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        IRequestClient<T> client = CreateRequestClient<T>(consumeContext, destinationAddress, timeout);

        return client.Create(values, cancellationToken: cancellationToken);
    }

    /// <summary>Creates request client.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <returns>The created request client.</returns>
    public IRequestClient<T> CreateRequestClient<T>(RequestTimeout timeout)
        where T : class
    {
        if (Context.MessageRoutes.TryGetDestinationAddress<T>(out var destinationAddress))
            return CreateRequestClient<T>(destinationAddress, timeout);

        return new RequestClient<T>(Context, Context.GetRequestEndpoint<T>(), timeout.Or(Context.DefaultTimeout));
    }

    /// <summary>Creates request client.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <returns>The created request client.</returns>
    public IRequestClient<T> CreateRequestClient<T>(ConsumeContext? consumeContext, RequestTimeout timeout)
        where T : class
    {
        if (Context.MessageRoutes.TryGetDestinationAddress<T>(out var destinationAddress))
            return CreateRequestClient<T>(consumeContext, destinationAddress, timeout);

        return new RequestClient<T>(Context, Context.GetRequestEndpoint<T>(consumeContext), timeout.Or(Context.DefaultTimeout));
    }

    /// <summary>Creates request client.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <returns>The created request client.</returns>
    public IRequestClient<T> CreateRequestClient<T>(Uri destinationAddress, RequestTimeout timeout)
        where T : class
    {
        IRequestSendEndpoint<T> requestSendEndpoint = Context.GetRequestEndpoint<T>(destinationAddress);

        return new RequestClient<T>(Context, requestSendEndpoint, timeout.Or(Context.DefaultTimeout));
    }

    /// <summary>Creates request client.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <returns>The created request client.</returns>
    public IRequestClient<T> CreateRequestClient<T>(ConsumeContext? consumeContext, Uri destinationAddress, RequestTimeout timeout)
        where T : class
    {
        return new RequestClient<T>(Context, Context.GetRequestEndpoint<T>(destinationAddress, consumeContext), timeout.Or(Context.DefaultTimeout));
    }
}
