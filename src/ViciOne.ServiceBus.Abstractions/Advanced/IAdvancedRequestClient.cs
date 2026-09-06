using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes request handles, callbacks, initializers, and multi-response operations.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
public interface IAdvancedRequestClient<TRequest>
    where TRequest : class
{
    /// <summary>Creates a request handle for a typed request.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The newly created instance.</returns>
    RequestHandle<TRequest> Create(TRequest message, RequestTimeout timeout = default, CancellationToken cancellationToken = default);

    /// <summary>Creates a request handle from initializer values.</summary>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The newly created instance.</returns>
    RequestHandle<TRequest> Create(object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default);

    /// <summary>Gets one response using a relative timeout.</summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where TResponse : class;

    /// <summary>Gets one response with advanced request-pipe configuration.</summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TResponse : class;

    /// <summary>Gets one initialized response using a relative timeout.</summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<Response<TResponse>> GetResponseAsync<TResponse>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where TResponse : class;

    /// <summary>Gets one initialized response with advanced request-pipe configuration.</summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="callback">The callback used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<Response<TResponse>> GetResponseAsync<TResponse>(object values, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TResponse : class;

    /// <summary>Gets either of two response types.</summary>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<Response<T1, T2>> GetResponseAsync<T1, T2>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class;

    /// <summary>Gets either of two response types with advanced request-pipe configuration.</summary>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<Response<T1, T2>> GetResponseAsync<T1, T2>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class;

    /// <summary>Gets either of two initialized response types.</summary>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<Response<T1, T2>> GetResponseAsync<T1, T2>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class;

    /// <summary>Gets either of two initialized response types with advanced request-pipe configuration.</summary>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="callback">The callback used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<Response<T1, T2>> GetResponseAsync<T1, T2>(object values, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class;

    /// <summary>Gets any of three response types.</summary>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <typeparam name="T3">The 3 type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<Response<T1, T2, T3>> GetResponseAsync<T1, T2, T3>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
        where T3 : class;

    /// <summary>Gets any of three response types with advanced request-pipe configuration.</summary>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <typeparam name="T3">The 3 type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<Response<T1, T2, T3>> GetResponseAsync<T1, T2, T3>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
        where T3 : class;

    /// <summary>Gets any of three initialized response types.</summary>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <typeparam name="T3">The 3 type.</typeparam>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<Response<T1, T2, T3>> GetResponseAsync<T1, T2, T3>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
        where T3 : class;

    /// <summary>Gets any of three initialized response types with advanced request-pipe configuration.</summary>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <typeparam name="T3">The 3 type.</typeparam>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="callback">The callback used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<Response<T1, T2, T3>> GetResponseAsync<T1, T2, T3>(object values, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T1 : class
        where T2 : class
        where T3 : class;
}
