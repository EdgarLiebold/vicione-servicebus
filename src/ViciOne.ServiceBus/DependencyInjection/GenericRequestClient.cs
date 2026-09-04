using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Mediator;

namespace ViciOne.ServiceBus.DependencyInjection;

public class GenericRequestClient<TRequest> :
    IRequestClient<TRequest>,
    Advanced.IAdvancedRequestClient<TRequest>
    where TRequest : class
{
    readonly IRequestClient<TRequest> _client;

    public GenericRequestClient(IServiceProvider provider)
    {
        _client = GetRequestClient(provider);
    }

    public Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TResponse : class
    {
        return _client.GetResponseAsync<TResponse>(request, cancellationToken);
    }

    public Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest request, RequestOptions options,
        CancellationToken cancellationToken = default)
        where TResponse : class
    {
        return _client.GetResponseAsync<TResponse>(request, options, cancellationToken);
    }

    public RequestHandle<TRequest> Create(TRequest message, CancellationToken cancellationToken, RequestTimeout timeout)
    {
        return _client.Advanced().Create(message, cancellationToken, timeout);
    }

    public RequestHandle<TRequest> Create(object values, CancellationToken cancellationToken, RequestTimeout timeout)
    {
        return _client.Advanced().Create(values, cancellationToken, timeout);
    }

    public Task<Response<T>> GetResponseAsync<T>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return _client.Advanced().GetResponseAsync<T>(message, timeout, cancellationToken);
    }

    public Task<Response<T>> GetResponseAsync<T>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return _client.Advanced().GetResponseAsync<T>(message, callback, timeout, cancellationToken);
    }

    public Task<Response<T>> GetResponseAsync<T>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return _client.Advanced().GetResponseAsync<T>(values, timeout, cancellationToken);
    }

    public Task<Response<T>> GetResponseAsync<T>(object values, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return _client.Advanced().GetResponseAsync<T>(values, callback, timeout, cancellationToken);
    }

    public Task<Response<T1, T2>> GetResponseAsync<T1, T2>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
    {
        return _client.Advanced().GetResponseAsync<T1, T2>(message, timeout, cancellationToken);
    }

    public Task<Response<T1, T2>> GetResponseAsync<T1, T2>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
    {
        return _client.Advanced().GetResponseAsync<T1, T2>(message, callback, timeout, cancellationToken);
    }

    public Task<Response<T1, T2>> GetResponseAsync<T1, T2>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
    {
        return _client.Advanced().GetResponseAsync<T1, T2>(values, timeout, cancellationToken);
    }

    public Task<Response<T1, T2>> GetResponseAsync<T1, T2>(object values, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
    {
        return _client.Advanced().GetResponseAsync<T1, T2>(values, callback, timeout, cancellationToken);
    }

    public Task<Response<T1, T2, T3>> GetResponseAsync<T1, T2, T3>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
        where T3 : class
    {
        return _client.Advanced().GetResponseAsync<T1, T2, T3>(message, timeout, cancellationToken);
    }

    public Task<Response<T1, T2, T3>> GetResponseAsync<T1, T2, T3>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
        where T3 : class
    {
        return _client.Advanced().GetResponseAsync<T1, T2, T3>(message, callback, timeout, cancellationToken);
    }

    public Task<Response<T1, T2, T3>> GetResponseAsync<T1, T2, T3>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
        where T3 : class
    {
        return _client.Advanced().GetResponseAsync<T1, T2, T3>(values, timeout, cancellationToken);
    }

    public Task<Response<T1, T2, T3>> GetResponseAsync<T1, T2, T3>(object values, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
        where T3 : class
    {
        return _client.Advanced().GetResponseAsync<T1, T2, T3>(values, callback, timeout, cancellationToken);
    }

    static IRequestClient<TRequest> GetRequestClient(IServiceProvider provider)
    {
        var clientFactory = provider.GetService<IScopedClientFactory>();
        if (clientFactory != null)
            return clientFactory.CreateRequestClient<TRequest>();

        var mediator = provider.GetService<IScopedMediator>();
        if (mediator != null)
        {
            var consumeContext = provider.GetRequiredService<Bind<IMediator, IScopedConsumeContextProvider>>().Value.GetContext();
            return mediator.CreateRequestClient<TRequest>(consumeContext);
        }

        throw new ViciOneServiceBusException($"Unable to resolve client factory or mediator for request client: {TypeCache<TRequest>.ShortName}");
    }
}
