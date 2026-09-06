using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Sends request messages and awaits one typed response.</summary>
public static class RequestExtensions
{
    /// <summary>Sends a request to an explicit endpoint and awaits the response.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="bus">A started bus instance.</param>
    /// <param name="destinationAddress">The service address.</param>
    /// <param name="message">The request message.</param>
    /// <param name="timeout">An optional timeout for the request (defaults to 30 seconds).</param>
    /// <param name="callback">A callback, which can modify the <see cref="SendContext" /> of the request.</param>
    /// <param name="cancellationToken">An optional cancellationToken for this request.</param>
    /// <returns>A task that produces the request outcome.</returns>
    public static async Task<Response<TResponse>> RequestAsync<TRequest, TResponse>(this IBus bus, Uri destinationAddress, TRequest message,
        RequestTimeout timeout = default, Action<SendContext<TRequest>>? callback = null, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(message);

        using RequestHandle<TRequest> requestHandle = bus
            .CreateRequestClient<TRequest>(destinationAddress, timeout)
            .Create(message, cancellationToken: cancellationToken);

        if (callback != null)
            requestHandle.UseExecute(callback);

        return await requestHandle.GetResponseAsync<TResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Initializes a request for an explicit endpoint and awaits the response.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="bus">A started bus instance.</param>
    /// <param name="destinationAddress">The service address.</param>
    /// <param name="values">The values used to initialize the request message.</param>
    /// <param name="timeout">An optional timeout for the request (defaults to 30 seconds).</param>
    /// <param name="callback">A callback, which can modify the <see cref="SendContext" /> of the request.</param>
    /// <param name="cancellationToken">An optional cancellationToken for this request.</param>
    /// <returns>A task that produces the request outcome.</returns>
    public static async Task<Response<TResponse>> RequestAsync<TRequest, TResponse>(this IBus bus, Uri destinationAddress, object values,
        RequestTimeout timeout = default, Action<SendContext<TRequest>>? callback = null, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(values);

        using RequestHandle<TRequest> requestHandle = bus
            .CreateRequestClient<TRequest>(destinationAddress, timeout)
            .Create(values, cancellationToken: cancellationToken);

        if (callback != null)
            requestHandle.UseExecute(callback);

        return await requestHandle.GetResponseAsync<TResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a request through the convention-based endpoint and awaits the response.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="bus">A started bus instance.</param>
    /// <param name="message">The request message.</param>
    /// <param name="timeout">An optional timeout for the request (defaults to 30 seconds).</param>
    /// <param name="callback">A callback, which can modify the <see cref="SendContext" /> of the request.</param>
    /// <param name="cancellationToken">An optional cancellationToken for this request.</param>
    /// <returns>A task that produces the request outcome.</returns>
    public static async Task<Response<TResponse>> RequestAsync<TRequest, TResponse>(this IBus bus, TRequest message,
        RequestTimeout timeout = default, Action<SendContext<TRequest>>? callback = null, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(message);

        using RequestHandle<TRequest> requestHandle = bus
            .CreateRequestClient<TRequest>(timeout)
            .Create(message, cancellationToken: cancellationToken);

        if (callback != null)
            requestHandle.UseExecute(callback);

        return await requestHandle.GetResponseAsync<TResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Initializes a request for the convention-based endpoint and awaits the response.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="bus">A started bus instance.</param>
    /// <param name="values">The values used to initialize the request message.</param>
    /// <param name="timeout">An optional timeout for the request (defaults to 30 seconds).</param>
    /// <param name="callback">A callback, which can modify the <see cref="SendContext" /> of the request.</param>
    /// <param name="cancellationToken">An optional cancellationToken for this request.</param>
    /// <returns>A task that produces the request outcome.</returns>
    public static async Task<Response<TResponse>> RequestAsync<TRequest, TResponse>(this IBus bus, object values, RequestTimeout timeout = default,
        Action<SendContext<TRequest>>? callback = null, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(values);

        using RequestHandle<TRequest> requestHandle = bus
            .CreateRequestClient<TRequest>(timeout)
            .Create(values, cancellationToken: cancellationToken);

        if (callback != null)
            requestHandle.UseExecute(callback);

        return await requestHandle.GetResponseAsync<TResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a request from a consume scope to an explicit endpoint and awaits the response.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="bus">A started bus instance.</param>
    /// <param name="destinationAddress">The service address.</param>
    /// <param name="message">The request message.</param>
    /// <param name="timeout">An optional timeout for the request (defaults to 30 seconds).</param>
    /// <param name="callback">A callback, which can modify the <see cref="SendContext" /> of the request.</param>
    /// <param name="cancellationToken">An optional cancellationToken for this request.</param>
    /// <returns>A task that produces the request outcome.</returns>
    public static async Task<Response<TResponse>> RequestAsync<TRequest, TResponse>(this ConsumeContext consumeContext, IBus bus, Uri destinationAddress,
        TRequest message, RequestTimeout timeout = default, Action<SendContext<TRequest>>? callback = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(message);

        using RequestHandle<TRequest> requestHandle = consumeContext
            .CreateRequestClient<TRequest>(bus, destinationAddress, timeout)
            .Create(message, cancellationToken: cancellationToken);

        if (callback != null)
            requestHandle.UseExecute(callback);

        return await requestHandle.GetResponseAsync<TResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Initializes a request from a consume scope for an explicit endpoint and awaits the response.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="bus">A started bus instance.</param>
    /// <param name="destinationAddress">The service address.</param>
    /// <param name="values">The values used to initialize the request message.</param>
    /// <param name="timeout">An optional timeout for the request (defaults to 30 seconds).</param>
    /// <param name="callback">A callback, which can modify the <see cref="SendContext" /> of the request.</param>
    /// <param name="cancellationToken">An optional cancellationToken for this request.</param>
    /// <returns>A task that produces the request outcome.</returns>
    public static async Task<Response<TResponse>> RequestAsync<TRequest, TResponse>(this ConsumeContext consumeContext, IBus bus, Uri destinationAddress,
        object values, RequestTimeout timeout = default, Action<SendContext<TRequest>>? callback = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(values);

        using RequestHandle<TRequest> requestHandle = consumeContext
            .CreateRequestClient<TRequest>(bus, destinationAddress, timeout)
            .Create(values, cancellationToken: cancellationToken);

        if (callback != null)
            requestHandle.UseExecute(callback);

        return await requestHandle.GetResponseAsync<TResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a request from a consume scope through the convention-based endpoint and awaits the response.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="bus">A started bus instance.</param>
    /// <param name="message">The request message.</param>
    /// <param name="timeout">An optional timeout for the request (defaults to 30 seconds).</param>
    /// <param name="callback">A callback, which can modify the <see cref="SendContext" /> of the request.</param>
    /// <param name="cancellationToken">An optional cancellationToken for this request.</param>
    /// <returns>A task that produces the request outcome.</returns>
    public static async Task<Response<TResponse>> RequestAsync<TRequest, TResponse>(this ConsumeContext consumeContext, IBus bus, TRequest message,
        RequestTimeout timeout = default, Action<SendContext<TRequest>>? callback = null, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(message);

        using RequestHandle<TRequest> requestHandle = consumeContext
            .CreateRequestClient<TRequest>(bus, timeout)
            .Create(message, cancellationToken: cancellationToken);

        if (callback != null)
            requestHandle.UseExecute(callback);

        return await requestHandle.GetResponseAsync<TResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Initializes a request from a consume scope for the convention-based endpoint and awaits the response.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="bus">A started bus instance.</param>
    /// <param name="values">The values used to initialize the request message.</param>
    /// <param name="timeout">An optional timeout for the request (defaults to 30 seconds).</param>
    /// <param name="callback">A callback, which can modify the <see cref="SendContext" /> of the request.</param>
    /// <param name="cancellationToken">An optional cancellationToken for this request.</param>
    /// <returns>A task that produces the request outcome.</returns>
    public static async Task<Response<TResponse>> RequestAsync<TRequest, TResponse>(this ConsumeContext consumeContext, IBus bus, object values,
        RequestTimeout timeout = default, Action<SendContext<TRequest>>? callback = null, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(values);

        using RequestHandle<TRequest> requestHandle = consumeContext
            .CreateRequestClient<TRequest>(bus, timeout)
            .Create(values, cancellationToken: cancellationToken);

        if (callback != null)
            requestHandle.UseExecute(callback);

        return await requestHandle.GetResponseAsync<TResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
