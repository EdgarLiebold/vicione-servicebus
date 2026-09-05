using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a generic request client implementation.
/// </summary>
/// <typeparam name="TRequest">The t request type.</typeparam>
public class GenericRequestClient<TRequest> :
    IRequestClient<TRequest>,
    Advanced.IAdvancedRequestClient<TRequest>
    where TRequest : class
{
    readonly IRequestClient<TRequest> _client;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    public GenericRequestClient(IServiceProvider provider)
    {
        _client = GetRequestClient(provider);
    }

    /// <summary>
    /// Gets response.
    /// </summary>
    /// <typeparam name="TResponse">The t response type.</typeparam>
    /// <param name="request">The request value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TResponse : class
    {
        return _client.GetResponseAsync<TResponse>(request, cancellationToken);
    }

    /// <summary>
    /// Gets response.
    /// </summary>
    /// <typeparam name="TResponse">The t response type.</typeparam>
    /// <param name="request">The request value.</param>
    /// <param name="options">The options value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest request, RequestOptions options,
        CancellationToken cancellationToken = default)
        where TResponse : class
    {
        return _client.GetResponseAsync<TResponse>(request, options, cancellationToken);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <returns>The result of the operation.</returns>
    public RequestHandle<TRequest> Create(TRequest message, CancellationToken cancellationToken, RequestTimeout timeout)
    {
        return _client.Advanced().Create(message, cancellationToken, timeout);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="values">The values value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <returns>The result of the operation.</returns>
    public RequestHandle<TRequest> Create(object values, CancellationToken cancellationToken, RequestTimeout timeout)
    {
        return _client.Advanced().Create(values, cancellationToken, timeout);
    }

    /// <summary>
    /// Gets response.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<Response<T>> GetResponseAsync<T>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return _client.Advanced().GetResponseAsync<T>(message, timeout, cancellationToken);
    }

    /// <summary>
    /// Gets response.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="callback">The callback value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<Response<T>> GetResponseAsync<T>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return _client.Advanced().GetResponseAsync<T>(message, callback, timeout, cancellationToken);
    }

    /// <summary>
    /// Gets response.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<Response<T>> GetResponseAsync<T>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return _client.Advanced().GetResponseAsync<T>(values, timeout, cancellationToken);
    }

    /// <summary>
    /// Gets response.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="callback">The callback value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<Response<T>> GetResponseAsync<T>(object values, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return _client.Advanced().GetResponseAsync<T>(values, callback, timeout, cancellationToken);
    }

    /// <summary>
    /// Gets response.
    /// </summary>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <typeparam name="T2">The t2 type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<Response<T1, T2>> GetResponseAsync<T1, T2>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
    {
        return _client.Advanced().GetResponseAsync<T1, T2>(message, timeout, cancellationToken);
    }

    /// <summary>
    /// Gets response.
    /// </summary>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <typeparam name="T2">The t2 type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="callback">The callback value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<Response<T1, T2>> GetResponseAsync<T1, T2>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
    {
        return _client.Advanced().GetResponseAsync<T1, T2>(message, callback, timeout, cancellationToken);
    }

    /// <summary>
    /// Gets response.
    /// </summary>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <typeparam name="T2">The t2 type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<Response<T1, T2>> GetResponseAsync<T1, T2>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
    {
        return _client.Advanced().GetResponseAsync<T1, T2>(values, timeout, cancellationToken);
    }

    /// <summary>
    /// Gets response.
    /// </summary>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <typeparam name="T2">The t2 type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="callback">The callback value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<Response<T1, T2>> GetResponseAsync<T1, T2>(object values, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
    {
        return _client.Advanced().GetResponseAsync<T1, T2>(values, callback, timeout, cancellationToken);
    }

    /// <summary>
    /// Gets response.
    /// </summary>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <typeparam name="T2">The t2 type.</typeparam>
    /// <typeparam name="T3">The t3 type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<Response<T1, T2, T3>> GetResponseAsync<T1, T2, T3>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
        where T3 : class
    {
        return _client.Advanced().GetResponseAsync<T1, T2, T3>(message, timeout, cancellationToken);
    }

    /// <summary>
    /// Gets response.
    /// </summary>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <typeparam name="T2">The t2 type.</typeparam>
    /// <typeparam name="T3">The t3 type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="callback">The callback value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<Response<T1, T2, T3>> GetResponseAsync<T1, T2, T3>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
        where T3 : class
    {
        return _client.Advanced().GetResponseAsync<T1, T2, T3>(message, callback, timeout, cancellationToken);
    }

    /// <summary>
    /// Gets response.
    /// </summary>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <typeparam name="T2">The t2 type.</typeparam>
    /// <typeparam name="T3">The t3 type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<Response<T1, T2, T3>> GetResponseAsync<T1, T2, T3>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
        where T3 : class
    {
        return _client.Advanced().GetResponseAsync<T1, T2, T3>(values, timeout, cancellationToken);
    }

    /// <summary>
    /// Gets response.
    /// </summary>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <typeparam name="T2">The t2 type.</typeparam>
    /// <typeparam name="T3">The t3 type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="callback">The callback value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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
        var clientFactory = (IScopedClientFactory?)provider.GetService(typeof(IScopedClientFactory));
        return clientFactory?.CreateRequestClient<TRequest>()
            ?? throw new ViciOneServiceBusException($"Unable to resolve a scoped client factory for request client: {TypeCache<TRequest>.ShortName}");
    }
}
