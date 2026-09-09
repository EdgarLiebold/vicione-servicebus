using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides callback, handle, timeout, and multi-response request operations.</summary>
public static class AdvancedRequestClientExtensions
{
    /// <summary>Returns the advanced request-client contract implemented by the client.</summary>
    /// <typeparam name="TRequest">The request contract type.</typeparam>
    /// <param name="client">The request client to expose as an infrastructure contract.</param>
    /// <returns>The client's advanced request contract.</returns>
    public static IAdvancedRequestClient<TRequest> Advanced<TRequest>(this IRequestClient<TRequest> client)
        where TRequest : class => RequireAdvanced(client);

    /// <summary>Creates a request handle for a typed request.</summary>
    /// <typeparam name="TRequest">The request contract type.</typeparam>
    /// <param name="client">The client that sends the request.</param>
    /// <param name="request">The request message.</param>
    /// <param name="timeout">The relative response timeout, or the client's configured default.</param>
    /// <param name="cancellationToken">The token that cancels the request lifecycle.</param>
    /// <returns>A handle used to register response types and observe request completion.</returns>
    public static RequestHandle<TRequest> Create<TRequest>(this IRequestClient<TRequest> client, TRequest request,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class => RequireRequest(client, request).Create(request, timeout, cancellationToken);

    /// <summary>Gets one response using a relative timeout.</summary>
    /// <typeparam name="TRequest">The request contract type.</typeparam>
    /// <typeparam name="TResponse">The accepted response contract type.</typeparam>
    /// <param name="client">The client that sends the request.</param>
    /// <param name="request">The request message.</param>
    /// <param name="timeout">The relative response timeout.</param>
    /// <param name="cancellationToken">The token that cancels the request lifecycle.</param>
    /// <returns>A task containing the received response and its message context.</returns>
    public static Task<Response<TResponse>> GetResponseAsync<TRequest, TResponse>(this IRequestClient<TRequest> client, TRequest request,
        RequestTimeout timeout, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class => RequireRequest(client, request).GetResponseAsync<TResponse>(request, timeout, cancellationToken);

    /// <summary>Gets one response with advanced request-pipe configuration.</summary>
    /// <typeparam name="TRequest">The request contract type.</typeparam>
    /// <typeparam name="TResponse">The accepted response contract type.</typeparam>
    /// <param name="client">The client that sends the request.</param>
    /// <param name="request">The request message.</param>
    /// <param name="callback">The callback that configures the request send pipeline.</param>
    /// <param name="timeout">The relative response timeout, or the client's configured default.</param>
    /// <param name="cancellationToken">The token that cancels the request lifecycle.</param>
    /// <returns>A task containing the received response and its message context.</returns>
    public static Task<Response<TResponse>> GetResponseAsync<TRequest, TResponse>(this IRequestClient<TRequest> client, TRequest request,
        RequestPipeConfiguratorCallback<TRequest> callback, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class => RequireRequest(client, request, callback).GetResponseAsync<TResponse>(request, callback, timeout, cancellationToken);

    /// <summary>Gets either of two response types.</summary>
    /// <typeparam name="TRequest">The request contract type.</typeparam>
    /// <typeparam name="TResponse1">The first accepted response contract type.</typeparam>
    /// <typeparam name="TResponse2">The second accepted response contract type.</typeparam>
    /// <param name="client">The client that sends the request.</param>
    /// <param name="request">The request message.</param>
    /// <param name="timeout">The relative response timeout, or the client's configured default.</param>
    /// <param name="cancellationToken">The token that cancels the request lifecycle.</param>
    /// <returns>A task containing the response branch that completed first.</returns>
    public static Task<Response<TResponse1, TResponse2>> GetResponseAsync<TRequest, TResponse1, TResponse2>(this IRequestClient<TRequest> client, TRequest request,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse1 : class
        where TResponse2 : class => RequireRequest(client, request).GetResponseAsync<TResponse1, TResponse2>(request, timeout, cancellationToken);

    /// <summary>Gets either of two response types with advanced request-pipe configuration.</summary>
    /// <typeparam name="TRequest">The request contract type.</typeparam>
    /// <typeparam name="TResponse1">The first accepted response contract type.</typeparam>
    /// <typeparam name="TResponse2">The second accepted response contract type.</typeparam>
    /// <param name="client">The client that sends the request.</param>
    /// <param name="request">The request message.</param>
    /// <param name="callback">The callback that configures the request send pipeline.</param>
    /// <param name="timeout">The relative response timeout, or the client's configured default.</param>
    /// <param name="cancellationToken">The token that cancels the request lifecycle.</param>
    /// <returns>A task containing the response branch that completed first.</returns>
    public static Task<Response<TResponse1, TResponse2>> GetResponseAsync<TRequest, TResponse1, TResponse2>(this IRequestClient<TRequest> client, TRequest request,
        RequestPipeConfiguratorCallback<TRequest> callback, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse1 : class
        where TResponse2 : class => RequireRequest(client, request, callback).GetResponseAsync<TResponse1, TResponse2>(request, callback, timeout, cancellationToken);

    /// <summary>Gets any of three response types.</summary>
    /// <typeparam name="TRequest">The request contract type.</typeparam>
    /// <typeparam name="TResponse1">The first accepted response contract type.</typeparam>
    /// <typeparam name="TResponse2">The second accepted response contract type.</typeparam>
    /// <typeparam name="TResponse3">The third accepted response contract type.</typeparam>
    /// <param name="client">The client that sends the request.</param>
    /// <param name="request">The request message.</param>
    /// <param name="timeout">The relative response timeout, or the client's configured default.</param>
    /// <param name="cancellationToken">The token that cancels the request lifecycle.</param>
    /// <returns>A task containing the response branch that completed first.</returns>
    public static Task<Response<TResponse1, TResponse2, TResponse3>> GetResponseAsync<TRequest, TResponse1, TResponse2, TResponse3>(this IRequestClient<TRequest> client, TRequest request,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse1 : class
        where TResponse2 : class
        where TResponse3 : class => RequireRequest(client, request).GetResponseAsync<TResponse1, TResponse2, TResponse3>(request, timeout, cancellationToken);

    /// <summary>Gets any of three response types with advanced request-pipe configuration.</summary>
    /// <typeparam name="TRequest">The request contract type.</typeparam>
    /// <typeparam name="TResponse1">The first accepted response contract type.</typeparam>
    /// <typeparam name="TResponse2">The second accepted response contract type.</typeparam>
    /// <typeparam name="TResponse3">The third accepted response contract type.</typeparam>
    /// <param name="client">The client that sends the request.</param>
    /// <param name="request">The request message.</param>
    /// <param name="callback">The callback that configures the request send pipeline.</param>
    /// <param name="timeout">The relative response timeout, or the client's configured default.</param>
    /// <param name="cancellationToken">The token that cancels the request lifecycle.</param>
    /// <returns>A task containing the response branch that completed first.</returns>
    public static Task<Response<TResponse1, TResponse2, TResponse3>> GetResponseAsync<TRequest, TResponse1, TResponse2, TResponse3>(this IRequestClient<TRequest> client, TRequest request,
        RequestPipeConfiguratorCallback<TRequest> callback, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse1 : class
        where TResponse2 : class
        where TResponse3 : class => RequireRequest(client, request, callback).GetResponseAsync<TResponse1, TResponse2, TResponse3>(request, callback, timeout, cancellationToken);

    static IAdvancedRequestClient<TRequest> RequireRequest<TRequest>(IRequestClient<TRequest> client, TRequest request)
        where TRequest : class
    {
        IAdvancedRequestClient<TRequest> advancedClient = RequireAdvanced(client);
        ArgumentNullException.ThrowIfNull(request);
        return advancedClient;
    }

    static IAdvancedRequestClient<TRequest> RequireRequest<TRequest>(IRequestClient<TRequest> client, TRequest request,
        RequestPipeConfiguratorCallback<TRequest> callback)
        where TRequest : class
    {
        IAdvancedRequestClient<TRequest> advancedClient = RequireRequest(client, request);
        ArgumentNullException.ThrowIfNull(callback);
        return advancedClient;
    }

    internal static IAdvancedRequestClient<TRequest> RequireAdvanced<TRequest>(IRequestClient<TRequest> client)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(client);
        return client as IAdvancedRequestClient<TRequest>
            ?? throw new NotSupportedException($"The request client '{client.GetType().FullName}' does not expose advanced request operations.");
    }
}
