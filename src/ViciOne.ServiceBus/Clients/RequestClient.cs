using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Implements request/response operations for one request contract.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
internal sealed class RequestClient<TRequest> :
    IRequestClient<TRequest>,
    Advanced.IAdvancedRequestClient<TRequest>
    where TRequest : class
{
    readonly ClientFactoryContext _context;
    readonly IRequestSendEndpoint<TRequest> _requestSendEndpoint;
    readonly RequestTimeout _timeout;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="requestSendEndpoint">The request send endpoint.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    public RequestClient(ClientFactoryContext context, IRequestSendEndpoint<TRequest> requestSendEndpoint, RequestTimeout timeout)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requestSendEndpoint);

        _context = context;
        _requestSendEndpoint = requestSendEndpoint;
        _timeout = timeout;
    }

    /// <summary>Gets response.</summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TResponse : class
    {
        return GetResponseAsync<TResponse>(request, timeout: default, cancellationToken);
    }

    /// <summary>Gets response.</summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest request, RequestOptions options,
        CancellationToken cancellationToken = default)
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(options);
        OutgoingOptionsSnapshot optionsSnapshot = OutgoingOptionsSnapshot.Create(options);

        RequestTimeout timeout = default;
        if (options.Deadline is { } deadline)
        {
            TimeSpan remaining = deadline - _context.TimeProvider.GetUtcNow();
            if (remaining <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(options), options.Deadline, "The request deadline must be in the future.");

            timeout = remaining;
        }

        async Task<TRequest> RequestAsync(Guid requestId, IPipe<SendContext<TRequest>> pipe, CancellationToken token)
        {
            await _requestSendEndpoint.SendAsync(requestId, request, pipe, token).ConfigureAwait(false);
            return request;
        }

        return GetResponseInternalAsync<TResponse>(
            RequestAsync,
            timeout,
            cancellationToken,
            configurator =>
            {
                if (optionsSnapshot.TimeToLive is { } timeToLive)
                    configurator.TimeToLive = timeToLive;

                configurator.UseExecute(context => OutgoingOptionsPipe.Apply(context, optionsSnapshot));
            },
            optionsSnapshot.RequestId);
    }

    /// <summary>Creates the requested value.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The newly created instance.</returns>
    public RequestHandle<TRequest> Create(TRequest message, RequestTimeout timeout, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        async Task<TRequest> RequestAsync(Guid requestId, IPipe<SendContext<TRequest>> pipe, CancellationToken token)
        {
            await _requestSendEndpoint.SendAsync(requestId, message, pipe, token).ConfigureAwait(false);

            return message;
        }

        return new ClientRequestHandle<TRequest>(_context, RequestAsync, cancellationToken, timeout.Or(_timeout));
    }

    /// <summary>Creates the requested value.</summary>
    /// <param name="values">The values.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The newly created instance.</returns>
    public RequestHandle<TRequest> Create(object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        async Task<TRequest> RequestAsync(Guid requestId, IPipe<SendContext<TRequest>> pipe, CancellationToken token)
        {
            return await _requestSendEndpoint.SendAsync(requestId, values, pipe, token).ConfigureAwait(false);
        }

        return new ClientRequestHandle<TRequest>(_context, RequestAsync, cancellationToken, timeout.Or(_timeout));
    }

    /// <summary>Gets response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Response<T>> GetResponseAsync<T>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return GetResponseAsync<T>(message, null, timeout, cancellationToken);
    }

    /// <summary>Gets response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Response<T>> GetResponseAsync<T>(TRequest message, RequestPipeConfiguratorCallback<TRequest>? callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);

        async Task<TRequest> RequestAsync(Guid requestId, IPipe<SendContext<TRequest>> pipe, CancellationToken token)
        {
            await _requestSendEndpoint.SendAsync(requestId, message, pipe, token).ConfigureAwait(false);

            return message;
        }

        return GetResponseInternalAsync<T>(RequestAsync, timeout, cancellationToken, callback);
    }

    /// <summary>Gets response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Response<T>> GetResponseAsync<T>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return GetResponseAsync<T>(values, null, timeout, cancellationToken);
    }

    /// <summary>Gets response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Response<T>> GetResponseAsync<T>(object values, RequestPipeConfiguratorCallback<TRequest>? callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        async Task<TRequest> RequestAsync(Guid requestId, IPipe<SendContext<TRequest>> pipe, CancellationToken token)
        {
            return await _requestSendEndpoint.SendAsync(requestId, values, pipe, token).ConfigureAwait(false);
        }

        return GetResponseInternalAsync<T>(RequestAsync, timeout, cancellationToken, callback);
    }

    /// <summary>Gets response.</summary>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Response<T1, T2>> GetResponseAsync<T1, T2>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
    {
        return GetResponseAsync<T1, T2>(message, null, timeout, cancellationToken);
    }

    /// <summary>Gets response.</summary>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Response<T1, T2>> GetResponseAsync<T1, T2>(TRequest message, RequestPipeConfiguratorCallback<TRequest>? callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
    {
        ArgumentNullException.ThrowIfNull(message);

        async Task<TRequest> RequestAsync(Guid requestId, IPipe<SendContext<TRequest>> pipe, CancellationToken token)
        {
            await _requestSendEndpoint.SendAsync(requestId, message, pipe, token).ConfigureAwait(false);

            return message;
        }

        return GetResponseInternalAsync<T1, T2>(RequestAsync, timeout, cancellationToken, callback);
    }

    /// <summary>Gets response.</summary>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Response<T1, T2>> GetResponseAsync<T1, T2>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
    {
        return GetResponseAsync<T1, T2>(values, null, timeout, cancellationToken);
    }

    /// <summary>Gets response.</summary>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Response<T1, T2>> GetResponseAsync<T1, T2>(object values, RequestPipeConfiguratorCallback<TRequest>? callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        async Task<TRequest> RequestAsync(Guid requestId, IPipe<SendContext<TRequest>> pipe, CancellationToken token)
        {
            return await _requestSendEndpoint.SendAsync(requestId, values, pipe, token).ConfigureAwait(false);
        }

        return GetResponseInternalAsync<T1, T2>(RequestAsync, timeout, cancellationToken, callback);
    }

    /// <summary>Gets response.</summary>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <typeparam name="T3">The 3 type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Response<T1, T2, T3>> GetResponseAsync<T1, T2, T3>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
        where T3 : class
    {
        return GetResponseAsync<T1, T2, T3>(message, null, timeout, cancellationToken);
    }

    /// <summary>Gets response.</summary>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <typeparam name="T3">The 3 type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Response<T1, T2, T3>> GetResponseAsync<T1, T2, T3>(TRequest message, RequestPipeConfiguratorCallback<TRequest>? callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
        where T3 : class
    {
        ArgumentNullException.ThrowIfNull(message);

        async Task<TRequest> RequestAsync(Guid requestId, IPipe<SendContext<TRequest>> pipe, CancellationToken token)
        {
            await _requestSendEndpoint.SendAsync(requestId, message, pipe, token).ConfigureAwait(false);

            return message;
        }

        return GetResponseInternalAsync<T1, T2, T3>(RequestAsync, timeout, cancellationToken, callback);
    }

    /// <summary>Gets response.</summary>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <typeparam name="T3">The 3 type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Response<T1, T2, T3>> GetResponseAsync<T1, T2, T3>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
        where T3 : class
    {
        return GetResponseAsync<T1, T2, T3>(values, null, timeout, cancellationToken);
    }

    /// <summary>Gets response.</summary>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <typeparam name="T3">The 3 type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Response<T1, T2, T3>> GetResponseAsync<T1, T2, T3>(object values, RequestPipeConfiguratorCallback<TRequest>? callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
        where T3 : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        async Task<TRequest> RequestAsync(Guid requestId, IPipe<SendContext<TRequest>> pipe, CancellationToken token)
        {
            return await _requestSendEndpoint.SendAsync(requestId, values, pipe, token).ConfigureAwait(false);
        }

        return GetResponseInternalAsync<T1, T2, T3>(RequestAsync, timeout, cancellationToken, callback);
    }

    async Task<Response<T>> GetResponseInternalAsync<T>(ClientRequestHandle<TRequest>.SendRequestCallback request,
        RequestTimeout timeout, CancellationToken cancellationToken, RequestPipeConfiguratorCallback<TRequest>? callback = null,
        Guid? requestId = null)
        where T : class
    {
        using RequestHandle<TRequest> handle = new ClientRequestHandle<TRequest>(
            _context,
            request,
            cancellationToken,
            timeout.Or(_timeout),
            requestId);

        callback?.Invoke(handle);

        return await handle.GetResponseAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    async Task<Response<T1, T2>> GetResponseInternalAsync<T1, T2>(ClientRequestHandle<TRequest>.SendRequestCallback request, RequestTimeout timeout, CancellationToken cancellationToken, RequestPipeConfiguratorCallback<TRequest>? callback = null)
        where T1 : class
        where T2 : class
    {
        using RequestHandle<TRequest> handle = new ClientRequestHandle<TRequest>(_context, request, cancellationToken, timeout.Or(_timeout));

        callback?.Invoke(handle);

        Task<Response<T1>> result1 = handle.GetResponseAsync<T1>(false, cancellationToken: cancellationToken);
        Task<Response<T2>> result2 = handle.GetResponseAsync<T2>(cancellationToken: cancellationToken);

        var task = await Task.WhenAny(result1, result2).ConfigureAwait(false);

        await task.ConfigureAwait(false);

        return new Response<T1, T2>(result1, result2);
    }

    async Task<Response<T1, T2, T3>> GetResponseInternalAsync<T1, T2, T3>(ClientRequestHandle<TRequest>.SendRequestCallback request,
        RequestTimeout timeout, CancellationToken cancellationToken, RequestPipeConfiguratorCallback<TRequest>? callback = null)
        where T1 : class
        where T2 : class
        where T3 : class
    {
        using RequestHandle<TRequest> handle = new ClientRequestHandle<TRequest>(_context, request, cancellationToken, timeout.Or(_timeout));

        callback?.Invoke(handle);

        Task<Response<T1>> result1 = handle.GetResponseAsync<T1>(false, cancellationToken: cancellationToken);
        Task<Response<T2>> result2 = handle.GetResponseAsync<T2>(false, cancellationToken: cancellationToken);
        Task<Response<T3>> result3 = handle.GetResponseAsync<T3>(cancellationToken: cancellationToken);

        var task = await Task.WhenAny(result1, result2, result3).ConfigureAwait(false);

        await task.ConfigureAwait(false);

        return new Response<T1, T2, T3>(result1, result2, result3);
    }
}
