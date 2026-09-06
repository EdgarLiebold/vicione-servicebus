using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Provides client operations for generic request.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
public class GenericRequestClient<TRequest> :
    IRequestClient<TRequest>,
    Advanced.IAdvancedRequestClient<TRequest>
    where TRequest : class
{
    readonly IRequestClient<TRequest> _client;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public GenericRequestClient(IServiceProvider provider)
    {
        _client = GetRequestClient(provider);
    }

    /// <summary>Gets response.</summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TResponse : class
    {
        return _client.GetResponseAsync<TResponse>(request, cancellationToken);
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
        return _client.GetResponseAsync<TResponse>(request, options, cancellationToken);
    }

    /// <summary>Creates the requested value.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The newly created instance.</returns>
    public RequestHandle<TRequest> Create(TRequest message, RequestTimeout timeout, CancellationToken cancellationToken)
    {
        return _client.Advanced().Create(message, timeout, cancellationToken);
    }

    /// <summary>Creates the requested value.</summary>
    /// <param name="values">The values.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The newly created instance.</returns>
    public RequestHandle<TRequest> Create(object values, RequestTimeout timeout, CancellationToken cancellationToken)
    {
        return _client.Advanced().Create(values, timeout, cancellationToken);
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
        return _client.Advanced().GetResponseAsync<T>(message, timeout, cancellationToken);
    }

    /// <summary>Gets response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Response<T>> GetResponseAsync<T>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return _client.Advanced().GetResponseAsync<T>(message, callback, timeout, cancellationToken);
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
        return _client.Advanced().GetResponseAsync<T>(values, timeout, cancellationToken);
    }

    /// <summary>Gets response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Response<T>> GetResponseAsync<T>(object values, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return _client.Advanced().GetResponseAsync<T>(values, callback, timeout, cancellationToken);
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
        return _client.Advanced().GetResponseAsync<T1, T2>(message, timeout, cancellationToken);
    }

    /// <summary>Gets response.</summary>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Response<T1, T2>> GetResponseAsync<T1, T2>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
    {
        return _client.Advanced().GetResponseAsync<T1, T2>(message, callback, timeout, cancellationToken);
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
        return _client.Advanced().GetResponseAsync<T1, T2>(values, timeout, cancellationToken);
    }

    /// <summary>Gets response.</summary>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Response<T1, T2>> GetResponseAsync<T1, T2>(object values, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
    {
        return _client.Advanced().GetResponseAsync<T1, T2>(values, callback, timeout, cancellationToken);
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
        return _client.Advanced().GetResponseAsync<T1, T2, T3>(message, timeout, cancellationToken);
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
    public Task<Response<T1, T2, T3>> GetResponseAsync<T1, T2, T3>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
        where T3 : class
    {
        return _client.Advanced().GetResponseAsync<T1, T2, T3>(message, callback, timeout, cancellationToken);
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
        return _client.Advanced().GetResponseAsync<T1, T2, T3>(values, timeout, cancellationToken);
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
