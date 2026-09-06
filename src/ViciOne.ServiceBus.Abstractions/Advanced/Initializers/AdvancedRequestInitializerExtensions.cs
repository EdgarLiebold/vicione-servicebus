using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Initializers;

/// <summary>Provides anonymous-value request initialization outside the application API namespace.</summary>
public static class AdvancedRequestInitializerExtensions
{
    /// <summary>Creates an initialized request handle.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="client">The client used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The newly created instance.</returns>
    public static RequestHandle<TRequest> Create<TRequest>(this IRequestClient<TRequest> client, object values,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class => AdvancedRequestClientExtensions.RequireAdvanced(client).Create(values, timeout, cancellationToken);

    /// <summary>Gets one initialized response using a relative timeout.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="client">The client used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public static Task<Response<TResponse>> GetResponseAsync<TRequest, TResponse>(this IRequestClient<TRequest> client, object values,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class => AdvancedRequestClientExtensions.RequireAdvanced(client)
            .GetResponseAsync<TResponse>(values, timeout, cancellationToken);

    /// <summary>Gets one initialized response with advanced request-pipe configuration.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="client">The client used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="callback">The callback used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public static Task<Response<TResponse>> GetResponseAsync<TRequest, TResponse>(this IRequestClient<TRequest> client, object values,
        RequestPipeConfiguratorCallback<TRequest> callback, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class => AdvancedRequestClientExtensions.RequireAdvanced(client)
            .GetResponseAsync<TResponse>(values, callback, timeout, cancellationToken);

    /// <summary>Gets either of two initialized response types.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <param name="client">The client used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public static Task<Response<T1, T2>> GetResponseAsync<TRequest, T1, T2>(this IRequestClient<TRequest> client, object values,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where T1 : class
        where T2 : class => AdvancedRequestClientExtensions.RequireAdvanced(client)
            .GetResponseAsync<T1, T2>(values, timeout, cancellationToken);

    /// <summary>Gets either of two initialized response types with advanced request-pipe configuration.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <param name="client">The client used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="callback">The callback used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public static Task<Response<T1, T2>> GetResponseAsync<TRequest, T1, T2>(this IRequestClient<TRequest> client, object values,
        RequestPipeConfiguratorCallback<TRequest> callback, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where T1 : class
        where T2 : class => AdvancedRequestClientExtensions.RequireAdvanced(client)
            .GetResponseAsync<T1, T2>(values, callback, timeout, cancellationToken);

    /// <summary>Gets any of three initialized response types.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <typeparam name="T3">The 3 type.</typeparam>
    /// <param name="client">The client used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public static Task<Response<T1, T2, T3>> GetResponseAsync<TRequest, T1, T2, T3>(this IRequestClient<TRequest> client, object values,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where T1 : class
        where T2 : class
        where T3 : class => AdvancedRequestClientExtensions.RequireAdvanced(client)
            .GetResponseAsync<T1, T2, T3>(values, timeout, cancellationToken);

    /// <summary>Gets any of three initialized response types with advanced request-pipe configuration.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <typeparam name="T2">The 2 type.</typeparam>
    /// <typeparam name="T3">The 3 type.</typeparam>
    /// <param name="client">The client used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="callback">The callback used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public static Task<Response<T1, T2, T3>> GetResponseAsync<TRequest, T1, T2, T3>(this IRequestClient<TRequest> client, object values,
        RequestPipeConfiguratorCallback<TRequest> callback, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where T1 : class
        where T2 : class
        where T3 : class => AdvancedRequestClientExtensions.RequireAdvanced(client)
            .GetResponseAsync<T1, T2, T3>(values, callback, timeout, cancellationToken);
}
