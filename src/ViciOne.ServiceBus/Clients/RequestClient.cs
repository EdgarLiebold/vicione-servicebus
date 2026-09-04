using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Clients;

public class RequestClient<TRequest> :
    IRequestClient<TRequest>,
    Advanced.IAdvancedRequestClient<TRequest>
    where TRequest : class
{
    readonly ClientFactoryContext _context;
    readonly IRequestSendEndpoint<TRequest> _requestSendEndpoint;
    readonly RequestTimeout _timeout;

    public RequestClient(ClientFactoryContext context, IRequestSendEndpoint<TRequest> requestSendEndpoint, RequestTimeout timeout)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requestSendEndpoint);

        _context = context;
        _requestSendEndpoint = requestSendEndpoint;
        _timeout = timeout;
    }

    public Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TResponse : class
    {
        return GetResponseAsync<TResponse>(request, timeout: default, cancellationToken);
    }

    public Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest request, RequestOptions options,
        CancellationToken cancellationToken = default)
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(options);

        RequestTimeout timeout = default;
        if (options.Deadline is { } deadline)
        {
            TimeSpan remaining = deadline - _context.TimeProvider.GetUtcNow();
            if (remaining <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(options), options.Deadline, "The request deadline must be in the future.");

            timeout = remaining;
        }

        return GetResponseAsync<TResponse>(
            request,
            configurator =>
            {
                if (options.TimeToLive is { } timeToLive)
                    configurator.TimeToLive = timeToLive;

                configurator.UseExecute(context => OutgoingOptionsPipe.Apply(
                    context,
                    options.Headers,
                    options.TimeToLive,
                    options.CorrelationId,
                    options.ConversationId,
                    options.MessageId,
                    options.RequestId,
                    options.PartitionKey));
            },
            timeout,
            cancellationToken);
    }

    public RequestHandle<TRequest> Create(TRequest message, CancellationToken cancellationToken, RequestTimeout timeout)
    {
        ArgumentNullException.ThrowIfNull(message);

        async Task<TRequest> RequestAsync(Guid requestId, IPipe<SendContext<TRequest>> pipe, CancellationToken token)
        {
            await _requestSendEndpoint.SendAsync(requestId, message, pipe, token).ConfigureAwait(false);

            return message;
        }

        return new ClientRequestHandle<TRequest>(_context, RequestAsync, cancellationToken, timeout.Or(_timeout));
    }

    public RequestHandle<TRequest> Create(object values, CancellationToken cancellationToken = default, RequestTimeout timeout = default)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        async Task<TRequest> RequestAsync(Guid requestId, IPipe<SendContext<TRequest>> pipe, CancellationToken token)
        {
            return await _requestSendEndpoint.SendAsync(requestId, values, pipe, token).ConfigureAwait(false);
        }

        return new ClientRequestHandle<TRequest>(_context, RequestAsync, cancellationToken, timeout.Or(_timeout));
    }

    public Task<Response<T>> GetResponseAsync<T>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return GetResponseAsync<T>(message, null, timeout, cancellationToken);
    }

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

        return GetResponseInternalAsync<T>(RequestAsync, cancellationToken, timeout, callback);
    }

    public Task<Response<T>> GetResponseAsync<T>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return GetResponseAsync<T>(values, null, timeout, cancellationToken);
    }

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

        return GetResponseInternalAsync<T>(RequestAsync, cancellationToken, timeout, callback);
    }

    public Task<Response<T1, T2>> GetResponseAsync<T1, T2>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
    {
        return GetResponseAsync<T1, T2>(message, null, timeout, cancellationToken);
    }

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

        return GetResponseInternalAsync<T1, T2>(RequestAsync, cancellationToken, timeout, callback);
    }

    public Task<Response<T1, T2>> GetResponseAsync<T1, T2>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
    {
        return GetResponseAsync<T1, T2>(values, null, timeout, cancellationToken);
    }

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

        return GetResponseInternalAsync<T1, T2>(RequestAsync, cancellationToken, timeout, callback);
    }

    public Task<Response<T1, T2, T3>> GetResponseAsync<T1, T2, T3>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
        where T3 : class
    {
        return GetResponseAsync<T1, T2, T3>(message, null, timeout, cancellationToken);
    }

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

        return GetResponseInternalAsync<T1, T2, T3>(RequestAsync, cancellationToken, timeout, callback);
    }

    public Task<Response<T1, T2, T3>> GetResponseAsync<T1, T2, T3>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
        where T3 : class
    {
        return GetResponseAsync<T1, T2, T3>(values, null, timeout, cancellationToken);
    }

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

        return GetResponseInternalAsync<T1, T2, T3>(RequestAsync, cancellationToken, timeout, callback);
    }

    async Task<Response<T>> GetResponseInternalAsync<T>(ClientRequestHandle<TRequest>.SendRequestCallback request,
        CancellationToken cancellationToken, RequestTimeout timeout, RequestPipeConfiguratorCallback<TRequest>? callback = null)
        where T : class
    {
        using RequestHandle<TRequest> handle = new ClientRequestHandle<TRequest>(_context, request, cancellationToken, timeout.Or(_timeout));

        callback?.Invoke(handle);

        return await handle.GetResponseAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    async Task<Response<T1, T2>> GetResponseInternalAsync<T1, T2>(ClientRequestHandle<TRequest>.SendRequestCallback request, CancellationToken cancellationToken,
        RequestTimeout timeout, RequestPipeConfiguratorCallback<TRequest>? callback = null)
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

    async Task<Response<T1, T2, T3>> GetResponseInternalAsync<T1, T2, T3>(ClientRequestHandle<TRequest>.SendRequestCallback request, CancellationToken
        cancellationToken, RequestTimeout timeout, RequestPipeConfiguratorCallback<TRequest>? callback = null)
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
