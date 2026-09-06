using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Initializers;

/// <summary>Provides advanced request operations that initialize request contracts from property values.</summary>
public static class AdvancedRequestInitializerExtensions
{
    /// <summary>Creates an initialized request handle.</summary>
    /// <typeparam name="TRequest">The request contract to initialize.</typeparam>
    /// <param name="client">The request client that creates the handle.</param>
    /// <param name="values">An object whose public properties provide the request values.</param>
    /// <param name="timeout">The maximum time to wait for a response.</param>
    /// <param name="cancellationToken">The token that cancels the request.</param>
    /// <returns>A handle that controls the initialized request.</returns>
    public static RequestHandle<TRequest> Create<TRequest>(this IRequestClient<TRequest> client, object values,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class => AdvancedRequestClientExtensions.RequireAdvanced(client).Create(values, timeout, cancellationToken);

    /// <summary>Gets one initialized response using a relative timeout.</summary>
    /// <typeparam name="TRequest">The request contract to initialize.</typeparam>
    /// <typeparam name="TResponse">The expected response contract.</typeparam>
    /// <param name="client">The request client.</param>
    /// <param name="values">An object whose public properties provide the request values.</param>
    /// <param name="timeout">The maximum time to wait for the response.</param>
    /// <param name="cancellationToken">The token that cancels the request.</param>
    /// <returns>A task that produces the received response.</returns>
    public static Task<Response<TResponse>> GetResponseAsync<TRequest, TResponse>(this IRequestClient<TRequest> client, object values,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class => AdvancedRequestClientExtensions.RequireAdvanced(client)
            .GetResponseAsync<TResponse>(values, timeout, cancellationToken);

    /// <summary>Gets one initialized response with advanced request-pipe configuration.</summary>
    /// <typeparam name="TRequest">The request contract to initialize.</typeparam>
    /// <typeparam name="TResponse">The expected response contract.</typeparam>
    /// <param name="client">The request client.</param>
    /// <param name="values">An object whose public properties provide the request values.</param>
    /// <param name="callback">The callback that configures the request send pipe.</param>
    /// <param name="timeout">The maximum time to wait for the response.</param>
    /// <param name="cancellationToken">The token that cancels the request.</param>
    /// <returns>A task that produces the received response.</returns>
    public static Task<Response<TResponse>> GetResponseAsync<TRequest, TResponse>(this IRequestClient<TRequest> client, object values,
        RequestPipeConfiguratorCallback<TRequest> callback, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class => AdvancedRequestClientExtensions.RequireAdvanced(client)
            .GetResponseAsync<TResponse>(values, callback, timeout, cancellationToken);

    /// <summary>Gets either of two initialized response types.</summary>
    /// <typeparam name="TRequest">The request contract to initialize.</typeparam>
    /// <typeparam name="T1">The first accepted response contract.</typeparam>
    /// <typeparam name="T2">The second accepted response contract.</typeparam>
    /// <param name="client">The request client.</param>
    /// <param name="values">An object whose public properties provide the request values.</param>
    /// <param name="timeout">The maximum time to wait for a response.</param>
    /// <param name="cancellationToken">The token that cancels the request.</param>
    /// <returns>A task that produces whichever accepted response arrives first.</returns>
    public static Task<Response<T1, T2>> GetResponseAsync<TRequest, T1, T2>(this IRequestClient<TRequest> client, object values,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where T1 : class
        where T2 : class => AdvancedRequestClientExtensions.RequireAdvanced(client)
            .GetResponseAsync<T1, T2>(values, timeout, cancellationToken);

    /// <summary>Gets either of two initialized response types with advanced request-pipe configuration.</summary>
    /// <typeparam name="TRequest">The request contract to initialize.</typeparam>
    /// <typeparam name="T1">The first accepted response contract.</typeparam>
    /// <typeparam name="T2">The second accepted response contract.</typeparam>
    /// <param name="client">The request client.</param>
    /// <param name="values">An object whose public properties provide the request values.</param>
    /// <param name="callback">The callback that configures the request send pipe.</param>
    /// <param name="timeout">The maximum time to wait for a response.</param>
    /// <param name="cancellationToken">The token that cancels the request.</param>
    /// <returns>A task that produces whichever accepted response arrives first.</returns>
    public static Task<Response<T1, T2>> GetResponseAsync<TRequest, T1, T2>(this IRequestClient<TRequest> client, object values,
        RequestPipeConfiguratorCallback<TRequest> callback, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where T1 : class
        where T2 : class => AdvancedRequestClientExtensions.RequireAdvanced(client)
            .GetResponseAsync<T1, T2>(values, callback, timeout, cancellationToken);

    /// <summary>Gets any of three initialized response types.</summary>
    /// <typeparam name="TRequest">The request contract to initialize.</typeparam>
    /// <typeparam name="T1">The first accepted response contract.</typeparam>
    /// <typeparam name="T2">The second accepted response contract.</typeparam>
    /// <typeparam name="T3">The third accepted response contract.</typeparam>
    /// <param name="client">The request client.</param>
    /// <param name="values">An object whose public properties provide the request values.</param>
    /// <param name="timeout">The maximum time to wait for a response.</param>
    /// <param name="cancellationToken">The token that cancels the request.</param>
    /// <returns>A task that produces whichever accepted response arrives first.</returns>
    public static Task<Response<T1, T2, T3>> GetResponseAsync<TRequest, T1, T2, T3>(this IRequestClient<TRequest> client, object values,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where T1 : class
        where T2 : class
        where T3 : class => AdvancedRequestClientExtensions.RequireAdvanced(client)
            .GetResponseAsync<T1, T2, T3>(values, timeout, cancellationToken);

    /// <summary>Gets any of three initialized response types with advanced request-pipe configuration.</summary>
    /// <typeparam name="TRequest">The request contract to initialize.</typeparam>
    /// <typeparam name="T1">The first accepted response contract.</typeparam>
    /// <typeparam name="T2">The second accepted response contract.</typeparam>
    /// <typeparam name="T3">The third accepted response contract.</typeparam>
    /// <param name="client">The request client.</param>
    /// <param name="values">An object whose public properties provide the request values.</param>
    /// <param name="callback">The callback that configures the request send pipe.</param>
    /// <param name="timeout">The maximum time to wait for a response.</param>
    /// <param name="cancellationToken">The token that cancels the request.</param>
    /// <returns>A task that produces whichever accepted response arrives first.</returns>
    public static Task<Response<T1, T2, T3>> GetResponseAsync<TRequest, T1, T2, T3>(this IRequestClient<TRequest> client, object values,
        RequestPipeConfiguratorCallback<TRequest> callback, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where T1 : class
        where T2 : class
        where T3 : class => AdvancedRequestClientExtensions.RequireAdvanced(client)
            .GetResponseAsync<T1, T2, T3>(values, callback, timeout, cancellationToken);
}
