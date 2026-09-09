using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Resolves the scoped request client for a contract and forwards its operations.</summary>
/// <typeparam name="TRequest">The request contract type.</typeparam>
public sealed class GenericRequestClient<TRequest> :
    IRequestClient<TRequest>,
    Advanced.IAdvancedRequestClient<TRequest>
    where TRequest : class
{
    readonly IRequestClient<TRequest> _client;

    /// <summary>Creates a forwarding client from the current dependency-injection scope.</summary>
    /// <param name="provider">The scoped service provider.</param>
    public GenericRequestClient(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _client = GetRequestClient(provider);
    }

    /// <inheritdoc />
    public Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TResponse : class
    {
        return _client.GetResponseAsync<TResponse>(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest request, RequestOptions options,
        CancellationToken cancellationToken = default)
        where TResponse : class
    {
        return _client.GetResponseAsync<TResponse>(request, options, cancellationToken);
    }

    /// <inheritdoc />
    public RequestHandle<TRequest> Create(TRequest message, RequestTimeout timeout, CancellationToken cancellationToken)
    {
        return _client.Advanced().Create(message, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public RequestHandle<TRequest> Create(object values, RequestTimeout timeout, CancellationToken cancellationToken)
    {
        return _client.Advanced().Create(values, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where TResponse : class
    {
        return _client.Advanced().GetResponseAsync<TResponse>(message, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TResponse : class
    {
        return _client.Advanced().GetResponseAsync<TResponse>(message, callback, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Response<TResponse>> GetResponseAsync<TResponse>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where TResponse : class
    {
        return _client.Advanced().GetResponseAsync<TResponse>(values, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Response<TResponse>> GetResponseAsync<TResponse>(object values, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TResponse : class
    {
        return _client.Advanced().GetResponseAsync<TResponse>(values, callback, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Response<TResponse1, TResponse2>> GetResponseAsync<TResponse1, TResponse2>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class
    {
        return _client.Advanced().GetResponseAsync<TResponse1, TResponse2>(message, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Response<TResponse1, TResponse2>> GetResponseAsync<TResponse1, TResponse2>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class
    {
        return _client.Advanced().GetResponseAsync<TResponse1, TResponse2>(message, callback, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Response<TResponse1, TResponse2>> GetResponseAsync<TResponse1, TResponse2>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class
    {
        return _client.Advanced().GetResponseAsync<TResponse1, TResponse2>(values, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Response<TResponse1, TResponse2>> GetResponseAsync<TResponse1, TResponse2>(object values, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class
    {
        return _client.Advanced().GetResponseAsync<TResponse1, TResponse2>(values, callback, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Response<TResponse1, TResponse2, TResponse3>> GetResponseAsync<TResponse1, TResponse2, TResponse3>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class
        where TResponse3 : class
    {
        return _client.Advanced().GetResponseAsync<TResponse1, TResponse2, TResponse3>(message, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Response<TResponse1, TResponse2, TResponse3>> GetResponseAsync<TResponse1, TResponse2, TResponse3>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class
        where TResponse3 : class
    {
        return _client.Advanced().GetResponseAsync<TResponse1, TResponse2, TResponse3>(message, callback, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Response<TResponse1, TResponse2, TResponse3>> GetResponseAsync<TResponse1, TResponse2, TResponse3>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class
        where TResponse3 : class
    {
        return _client.Advanced().GetResponseAsync<TResponse1, TResponse2, TResponse3>(values, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Response<TResponse1, TResponse2, TResponse3>> GetResponseAsync<TResponse1, TResponse2, TResponse3>(object values, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class
        where TResponse3 : class
    {
        return _client.Advanced().GetResponseAsync<TResponse1, TResponse2, TResponse3>(values, callback, timeout, cancellationToken);
    }

    static IRequestClient<TRequest> GetRequestClient(IServiceProvider provider)
    {
        var clientFactory = (IScopedClientFactory?)provider.GetService(typeof(IScopedClientFactory));
        return clientFactory?.CreateRequestClient<TRequest>()
            ?? throw new ViciOneServiceBusException($"Unable to resolve a scoped client factory for request client: {TypeCache<TRequest>.ShortName}");
    }
}
