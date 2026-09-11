using System;
using System.Threading;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Creates request handles and reusable request clients over a shared response endpoint.</summary>
public interface IClientFactory : IAsyncDisposable
{
    /// <summary>Gets the routing and response context used by this factory.</summary>
    ClientFactoryContext Context { get; }

    /// <summary>Creates a request handle that uses the configured route or publish fallback.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="message">The request message.</param>
    /// <param name="timeout">The response timeout, or an unspecified value to use the factory default.</param>
    /// <param name="cancellationToken">Cancels sending and response waiting.</param>
    /// <returns>A handle used to configure the request and await its response.</returns>
    RequestHandle<T> CreateRequest<T>(T message, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Creates a request handle for an explicit destination.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="destinationAddress">The address to which the request is sent.</param>
    /// <param name="message">The request message.</param>
    /// <param name="timeout">The response timeout, or an unspecified value to use the factory default.</param>
    /// <param name="cancellationToken">Cancels sending and response waiting.</param>
    /// <returns>A handle used to configure the request and await its response.</returns>
    RequestHandle<T> CreateRequest<T>(Uri destinationAddress, T message, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Creates a routed request handle that propagates metadata from a consumed message.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="consumeContext">The consumed message whose outbound metadata is propagated.</param>
    /// <param name="message">The request message.</param>
    /// <param name="timeout">The response timeout, or an unspecified value to use the factory default.</param>
    /// <param name="cancellationToken">Cancels sending and response waiting.</param>
    /// <returns>A handle used to configure the request and await its response.</returns>
    RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, T message, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Creates a request handle for an explicit destination and propagates consumed-message metadata.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="consumeContext">The consumed message whose outbound metadata is propagated.</param>
    /// <param name="destinationAddress">The address to which the request is sent.</param>
    /// <param name="message">The request message.</param>
    /// <param name="timeout">The response timeout, or an unspecified value to use the factory default.</param>
    /// <param name="cancellationToken">Cancels sending and response waiting.</param>
    /// <returns>A handle used to configure the request and await its response.</returns>
    RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, Uri destinationAddress, T message, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Creates an initialized request handle that uses the configured route or publish fallback.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="values">The values to initialize the message.</param>
    /// <param name="timeout">The response timeout, or an unspecified value to use the factory default.</param>
    /// <param name="cancellationToken">Cancels initialization, sending, and response waiting.</param>
    /// <returns>A handle used to configure the request and await its response.</returns>
    RequestHandle<T> CreateRequest<T>(object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Creates an initialized request handle for an explicit destination.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="destinationAddress">The address to which the request is sent.</param>
    /// <param name="values">The values to initialize the message.</param>
    /// <param name="timeout">The response timeout, or an unspecified value to use the factory default.</param>
    /// <param name="cancellationToken">Cancels initialization, sending, and response waiting.</param>
    /// <returns>A handle used to configure the request and await its response.</returns>
    RequestHandle<T> CreateRequest<T>(Uri destinationAddress, object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Creates an initialized routed request and propagates consumed-message metadata.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="consumeContext">The consumed message whose outbound metadata is propagated.</param>
    /// <param name="values">The values to initialize the message.</param>
    /// <param name="timeout">The response timeout, or an unspecified value to use the factory default.</param>
    /// <param name="cancellationToken">Cancels initialization, sending, and response waiting.</param>
    /// <returns>A handle used to configure the request and await its response.</returns>
    RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Creates an initialized request for an explicit destination and propagates consumed-message metadata.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="consumeContext">The consumed message whose outbound metadata is propagated.</param>
    /// <param name="destinationAddress">The address to which the request is sent.</param>
    /// <param name="values">The values to initialize the message.</param>
    /// <param name="timeout">The response timeout, or an unspecified value to use the factory default.</param>
    /// <param name="cancellationToken">Cancels initialization, sending, and response waiting.</param>
    /// <returns>A handle used to configure the request and await its response.</returns>
    RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, Uri destinationAddress, object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Creates a request client that uses the configured route or publish fallback.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="timeout">The default response timeout, or an unspecified value to use the factory default.</param>
    /// <returns>A request client for <typeparamref name="T" />.</returns>
    IRequestClient<T> CreateRequestClient<T>(RequestTimeout timeout = default)
        where T : class;

    /// <summary>Creates a routed request client that propagates metadata from a consumed message.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="consumeContext">The consumed message whose outbound metadata is propagated.</param>
    /// <param name="timeout">The default response timeout, or an unspecified value to use the factory default.</param>
    /// <returns>A request client for <typeparamref name="T" />.</returns>
    IRequestClient<T> CreateRequestClient<T>(ConsumeContext consumeContext, RequestTimeout timeout = default)
        where T : class;

    /// <summary>Creates a request client for an explicit destination.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="destinationAddress">The address to which requests are sent.</param>
    /// <param name="timeout">The default response timeout, or an unspecified value to use the factory default.</param>
    /// <returns>A request client for <typeparamref name="T" />.</returns>
    IRequestClient<T> CreateRequestClient<T>(Uri destinationAddress, RequestTimeout timeout = default)
        where T : class;

    /// <summary>Creates a request client for an explicit destination and propagates consumed-message metadata.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="consumeContext">The consumed message whose outbound metadata is propagated.</param>
    /// <param name="destinationAddress">The address to which requests are sent.</param>
    /// <param name="timeout">The default response timeout, or an unspecified value to use the factory default.</param>
    /// <returns>A request client for <typeparamref name="T" />.</returns>
    IRequestClient<T> CreateRequestClient<T>(ConsumeContext consumeContext, Uri destinationAddress, RequestTimeout timeout = default)
        where T : class;
}
