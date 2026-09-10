using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Implements request/response operations for one request contract.</summary>
/// <typeparam name="TRequest">The request message contract.</typeparam>
internal sealed class RequestClient<TRequest> :
    IRequestClient<TRequest>,
    Advanced.IAdvancedRequestClient<TRequest>
    where TRequest : class
{
    readonly ClientFactoryContext _context;
    readonly IRequestSendEndpoint<TRequest> _requestSendEndpoint;
    readonly RequestTimeout _timeout;

    /// <summary>Creates a request client for one request contract.</summary>
    /// <param name="context">The factory context that supplies response endpoints, logging, scheduling, and time.</param>
    /// <param name="requestSendEndpoint">The request send endpoint.</param>
    /// <param name="timeout">The default response timeout.</param>
    public RequestClient(ClientFactoryContext context, IRequestSendEndpoint<TRequest> requestSendEndpoint, RequestTimeout timeout)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requestSendEndpoint);

        _context = context;
        _requestSendEndpoint = requestSendEndpoint;
        _timeout = timeout;
    }

    /// <inheritdoc />
    public Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TResponse : class
    {
        return GetResponseAsync<TResponse>(request, timeout: default, cancellationToken);
    }

    /// <inheritdoc />
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

            timeout = new RequestTimeout(remaining);
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
                    configurator.TimeToLive = new RequestTimeout(timeToLive);

                configurator.UseExecute(context => OutgoingOptionsPipe.Apply(context, optionsSnapshot));
            },
            optionsSnapshot.RequestId);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public Task<Response<T>> GetResponseAsync<T>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return GetResponseAsync<T>(message, null, timeout, cancellationToken);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public Task<Response<T>> GetResponseAsync<T>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return GetResponseAsync<T>(values, null, timeout, cancellationToken);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public Task<Response<TResponse1, TResponse2>> GetResponseAsync<TResponse1, TResponse2>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class
    {
        return GetResponseAsync<TResponse1, TResponse2>(message, null, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Response<TResponse1, TResponse2>> GetResponseAsync<TResponse1, TResponse2>(TRequest message, RequestPipeConfiguratorCallback<TRequest>? callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class
    {
        ArgumentNullException.ThrowIfNull(message);

        async Task<TRequest> RequestAsync(Guid requestId, IPipe<SendContext<TRequest>> pipe, CancellationToken token)
        {
            await _requestSendEndpoint.SendAsync(requestId, message, pipe, token).ConfigureAwait(false);

            return message;
        }

        return GetResponseInternalAsync<TResponse1, TResponse2>(RequestAsync, timeout, cancellationToken, callback);
    }

    /// <inheritdoc />
    public Task<Response<TResponse1, TResponse2>> GetResponseAsync<TResponse1, TResponse2>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class
    {
        return GetResponseAsync<TResponse1, TResponse2>(values, null, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Response<TResponse1, TResponse2>> GetResponseAsync<TResponse1, TResponse2>(object values, RequestPipeConfiguratorCallback<TRequest>? callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        async Task<TRequest> RequestAsync(Guid requestId, IPipe<SendContext<TRequest>> pipe, CancellationToken token)
        {
            return await _requestSendEndpoint.SendAsync(requestId, values, pipe, token).ConfigureAwait(false);
        }

        return GetResponseInternalAsync<TResponse1, TResponse2>(RequestAsync, timeout, cancellationToken, callback);
    }

    /// <inheritdoc />
    public Task<Response<TResponse1, TResponse2, TResponse3>> GetResponseAsync<TResponse1, TResponse2, TResponse3>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class
        where TResponse3 : class
    {
        return GetResponseAsync<TResponse1, TResponse2, TResponse3>(message, null, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Response<TResponse1, TResponse2, TResponse3>> GetResponseAsync<TResponse1, TResponse2, TResponse3>(TRequest message, RequestPipeConfiguratorCallback<TRequest>? callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class
        where TResponse3 : class
    {
        ArgumentNullException.ThrowIfNull(message);

        async Task<TRequest> RequestAsync(Guid requestId, IPipe<SendContext<TRequest>> pipe, CancellationToken token)
        {
            await _requestSendEndpoint.SendAsync(requestId, message, pipe, token).ConfigureAwait(false);

            return message;
        }

        return GetResponseInternalAsync<TResponse1, TResponse2, TResponse3>(RequestAsync, timeout, cancellationToken, callback);
    }

    /// <inheritdoc />
    public Task<Response<TResponse1, TResponse2, TResponse3>> GetResponseAsync<TResponse1, TResponse2, TResponse3>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class
        where TResponse3 : class
    {
        return GetResponseAsync<TResponse1, TResponse2, TResponse3>(values, null, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Response<TResponse1, TResponse2, TResponse3>> GetResponseAsync<TResponse1, TResponse2, TResponse3>(object values, RequestPipeConfiguratorCallback<TRequest>? callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class
        where TResponse3 : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        async Task<TRequest> RequestAsync(Guid requestId, IPipe<SendContext<TRequest>> pipe, CancellationToken token)
        {
            return await _requestSendEndpoint.SendAsync(requestId, values, pipe, token).ConfigureAwait(false);
        }

        return GetResponseInternalAsync<TResponse1, TResponse2, TResponse3>(RequestAsync, timeout, cancellationToken, callback);
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

    async Task<Response<TResponse1, TResponse2>> GetResponseInternalAsync<TResponse1, TResponse2>(
        ClientRequestHandle<TRequest>.SendRequestCallback request,
        RequestTimeout timeout,
        CancellationToken cancellationToken,
        RequestPipeConfiguratorCallback<TRequest>? callback = null)
        where TResponse1 : class
        where TResponse2 : class
    {
        using RequestHandle<TRequest> handle = new ClientRequestHandle<TRequest>(_context, request, cancellationToken, timeout.Or(_timeout));

        callback?.Invoke(handle);

        Task<Response<TResponse1>> result1 = handle.GetResponseAsync<TResponse1>(false, cancellationToken: cancellationToken);
        Task<Response<TResponse2>> result2 = handle.GetResponseAsync<TResponse2>(cancellationToken: cancellationToken);

        var task = await Task.WhenAny(result1, result2).ConfigureAwait(false);

        await task.ConfigureAwait(false);

        return new Response<TResponse1, TResponse2>(result1, result2);
    }

    async Task<Response<TResponse1, TResponse2, TResponse3>> GetResponseInternalAsync<TResponse1, TResponse2, TResponse3>(ClientRequestHandle<TRequest>.SendRequestCallback request,
        RequestTimeout timeout, CancellationToken cancellationToken, RequestPipeConfiguratorCallback<TRequest>? callback = null)
        where TResponse1 : class
        where TResponse2 : class
        where TResponse3 : class
    {
        using RequestHandle<TRequest> handle = new ClientRequestHandle<TRequest>(_context, request, cancellationToken, timeout.Or(_timeout));

        callback?.Invoke(handle);

        Task<Response<TResponse1>> result1 = handle.GetResponseAsync<TResponse1>(false, cancellationToken: cancellationToken);
        Task<Response<TResponse2>> result2 = handle.GetResponseAsync<TResponse2>(false, cancellationToken: cancellationToken);
        Task<Response<TResponse3>> result3 = handle.GetResponseAsync<TResponse3>(cancellationToken: cancellationToken);

        var task = await Task.WhenAny(result1, result2, result3).ConfigureAwait(false);

        await task.ConfigureAwait(false);

        return new Response<TResponse1, TResponse2, TResponse3>(result1, result2, result3);
    }
}
