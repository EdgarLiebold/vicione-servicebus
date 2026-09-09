using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes request handles, callbacks, initializers, and multi-response operations.</summary>
/// <typeparam name="TRequest">The request contract type.</typeparam>
public interface IAdvancedRequestClient<TRequest>
    where TRequest : class
{
    /// <summary>Creates a request handle for a typed request.</summary>
    /// <param name="message">The request message.</param>
    /// <param name="timeout">The relative response timeout, or the client's configured default.</param>
    /// <param name="cancellationToken">The token that cancels the request lifecycle.</param>
    /// <returns>A handle used to register response types and observe request completion.</returns>
    RequestHandle<TRequest> Create(TRequest message, RequestTimeout timeout = default, CancellationToken cancellationToken = default);

    /// <summary>Creates a request handle from initializer values.</summary>
    /// <param name="values">The property values used to initialize the request message.</param>
    /// <param name="timeout">The relative response timeout, or the client's configured default.</param>
    /// <param name="cancellationToken">The token that cancels initialization or the request lifecycle.</param>
    /// <returns>A handle used to register response types and observe request completion.</returns>
    RequestHandle<TRequest> Create(object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default);

    /// <summary>Gets one response using a relative timeout.</summary>
    /// <typeparam name="TResponse">The accepted response contract type.</typeparam>
    /// <param name="message">The request message.</param>
    /// <param name="timeout">The relative response timeout, or the client's configured default.</param>
    /// <param name="cancellationToken">The token that cancels the request lifecycle.</param>
    /// <returns>A task containing the received response and its message context.</returns>
    Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where TResponse : class;

    /// <summary>Gets one response with advanced request-pipe configuration.</summary>
    /// <typeparam name="TResponse">The accepted response contract type.</typeparam>
    /// <param name="message">The request message.</param>
    /// <param name="callback">The callback that configures the request send pipeline.</param>
    /// <param name="timeout">The relative response timeout, or the client's configured default.</param>
    /// <param name="cancellationToken">The token that cancels the request lifecycle.</param>
    /// <returns>A task containing the received response and its message context.</returns>
    Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TResponse : class;

    /// <summary>Gets one initialized response using a relative timeout.</summary>
    /// <typeparam name="TResponse">The accepted response contract type.</typeparam>
    /// <param name="values">The property values used to initialize the request message.</param>
    /// <param name="timeout">The relative response timeout, or the client's configured default.</param>
    /// <param name="cancellationToken">The token that cancels initialization or the request lifecycle.</param>
    /// <returns>A task containing the received response and its message context.</returns>
    Task<Response<TResponse>> GetResponseAsync<TResponse>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where TResponse : class;

    /// <summary>Gets one initialized response with advanced request-pipe configuration.</summary>
    /// <typeparam name="TResponse">The accepted response contract type.</typeparam>
    /// <param name="values">The property values used to initialize the request message.</param>
    /// <param name="callback">The callback that configures the request send pipeline.</param>
    /// <param name="timeout">The relative response timeout, or the client's configured default.</param>
    /// <param name="cancellationToken">The token that cancels initialization or the request lifecycle.</param>
    /// <returns>A task containing the received response and its message context.</returns>
    Task<Response<TResponse>> GetResponseAsync<TResponse>(object values, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TResponse : class;

    /// <summary>Gets either of two response types.</summary>
    /// <typeparam name="TResponse1">The first accepted response contract type.</typeparam>
    /// <typeparam name="TResponse2">The second accepted response contract type.</typeparam>
    /// <param name="message">The request message.</param>
    /// <param name="timeout">The relative response timeout, or the client's configured default.</param>
    /// <param name="cancellationToken">The token that cancels the request lifecycle.</param>
    /// <returns>A task containing the response branch that completed first.</returns>
    Task<Response<TResponse1, TResponse2>> GetResponseAsync<TResponse1, TResponse2>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class;

    /// <summary>Gets either of two response types with advanced request-pipe configuration.</summary>
    /// <typeparam name="TResponse1">The first accepted response contract type.</typeparam>
    /// <typeparam name="TResponse2">The second accepted response contract type.</typeparam>
    /// <param name="message">The request message.</param>
    /// <param name="callback">The callback that configures the request send pipeline.</param>
    /// <param name="timeout">The relative response timeout, or the client's configured default.</param>
    /// <param name="cancellationToken">The token that cancels the request lifecycle.</param>
    /// <returns>A task containing the response branch that completed first.</returns>
    Task<Response<TResponse1, TResponse2>> GetResponseAsync<TResponse1, TResponse2>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class;

    /// <summary>Gets either of two initialized response types.</summary>
    /// <typeparam name="TResponse1">The first accepted response contract type.</typeparam>
    /// <typeparam name="TResponse2">The second accepted response contract type.</typeparam>
    /// <param name="values">The property values used to initialize the request message.</param>
    /// <param name="timeout">The relative response timeout, or the client's configured default.</param>
    /// <param name="cancellationToken">The token that cancels initialization or the request lifecycle.</param>
    /// <returns>A task containing the response branch that completed first.</returns>
    Task<Response<TResponse1, TResponse2>> GetResponseAsync<TResponse1, TResponse2>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class;

    /// <summary>Gets either of two initialized response types with advanced request-pipe configuration.</summary>
    /// <typeparam name="TResponse1">The first accepted response contract type.</typeparam>
    /// <typeparam name="TResponse2">The second accepted response contract type.</typeparam>
    /// <param name="values">The property values used to initialize the request message.</param>
    /// <param name="callback">The callback that configures the request send pipeline.</param>
    /// <param name="timeout">The relative response timeout, or the client's configured default.</param>
    /// <param name="cancellationToken">The token that cancels initialization or the request lifecycle.</param>
    /// <returns>A task containing the response branch that completed first.</returns>
    Task<Response<TResponse1, TResponse2>> GetResponseAsync<TResponse1, TResponse2>(object values, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class;

    /// <summary>Gets any of three response types.</summary>
    /// <typeparam name="TResponse1">The first accepted response contract type.</typeparam>
    /// <typeparam name="TResponse2">The second accepted response contract type.</typeparam>
    /// <typeparam name="TResponse3">The third accepted response contract type.</typeparam>
    /// <param name="message">The request message.</param>
    /// <param name="timeout">The relative response timeout, or the client's configured default.</param>
    /// <param name="cancellationToken">The token that cancels the request lifecycle.</param>
    /// <returns>A task containing the response branch that completed first.</returns>
    Task<Response<TResponse1, TResponse2, TResponse3>> GetResponseAsync<TResponse1, TResponse2, TResponse3>(TRequest message, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class
        where TResponse3 : class;

    /// <summary>Gets any of three response types with advanced request-pipe configuration.</summary>
    /// <typeparam name="TResponse1">The first accepted response contract type.</typeparam>
    /// <typeparam name="TResponse2">The second accepted response contract type.</typeparam>
    /// <typeparam name="TResponse3">The third accepted response contract type.</typeparam>
    /// <param name="message">The request message.</param>
    /// <param name="callback">The callback that configures the request send pipeline.</param>
    /// <param name="timeout">The relative response timeout, or the client's configured default.</param>
    /// <param name="cancellationToken">The token that cancels the request lifecycle.</param>
    /// <returns>A task containing the response branch that completed first.</returns>
    Task<Response<TResponse1, TResponse2, TResponse3>> GetResponseAsync<TResponse1, TResponse2, TResponse3>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class
        where TResponse3 : class;

    /// <summary>Gets any of three initialized response types.</summary>
    /// <typeparam name="TResponse1">The first accepted response contract type.</typeparam>
    /// <typeparam name="TResponse2">The second accepted response contract type.</typeparam>
    /// <typeparam name="TResponse3">The third accepted response contract type.</typeparam>
    /// <param name="values">The property values used to initialize the request message.</param>
    /// <param name="timeout">The relative response timeout, or the client's configured default.</param>
    /// <param name="cancellationToken">The token that cancels initialization or the request lifecycle.</param>
    /// <returns>A task containing the response branch that completed first.</returns>
    Task<Response<TResponse1, TResponse2, TResponse3>> GetResponseAsync<TResponse1, TResponse2, TResponse3>(object values, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class
        where TResponse3 : class;

    /// <summary>Gets any of three initialized response types with advanced request-pipe configuration.</summary>
    /// <typeparam name="TResponse1">The first accepted response contract type.</typeparam>
    /// <typeparam name="TResponse2">The second accepted response contract type.</typeparam>
    /// <typeparam name="TResponse3">The third accepted response contract type.</typeparam>
    /// <param name="values">The property values used to initialize the request message.</param>
    /// <param name="callback">The callback that configures the request send pipeline.</param>
    /// <param name="timeout">The relative response timeout, or the client's configured default.</param>
    /// <param name="cancellationToken">The token that cancels initialization or the request lifecycle.</param>
    /// <returns>A task containing the response branch that completed first.</returns>
    Task<Response<TResponse1, TResponse2, TResponse3>> GetResponseAsync<TResponse1, TResponse2, TResponse3>(object values, RequestPipeConfiguratorCallback<TRequest> callback,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TResponse1 : class
        where TResponse2 : class
        where TResponse3 : class;
}
