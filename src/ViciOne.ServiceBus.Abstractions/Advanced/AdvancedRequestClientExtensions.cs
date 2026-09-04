using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides callback, handle, timeout, and multi-response request operations.</summary>
public static class AdvancedRequestClientExtensions
{
    /// <summary>Returns the advanced request-client contract implemented by the client.</summary>
    public static IAdvancedRequestClient<TRequest> Advanced<TRequest>(this IRequestClient<TRequest> client)
        where TRequest : class => RequireAdvanced(client);

    /// <summary>Creates a request handle for a typed request.</summary>
    /// <param name="client">The client used by the operation.</param>
    /// <param name="request">The request used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    public static RequestHandle<TRequest> Create<TRequest>(this IRequestClient<TRequest> client, TRequest request,
        CancellationToken cancellationToken = default, RequestTimeout timeout = default)
        where TRequest : class => RequireAdvanced(client).Create(request, cancellationToken, timeout);

    /// <summary>Gets one response using a legacy relative timeout.</summary>
    /// <param name="client">The client used by the operation.</param>
    /// <param name="request">The request used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task<Response<TResponse>> GetResponseAsync<TRequest, TResponse>(this IRequestClient<TRequest> client, TRequest request,
        RequestTimeout timeout, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class => RequireAdvanced(client).GetResponseAsync<TResponse>(request, timeout, cancellationToken);

    /// <summary>Gets one response with advanced request-pipe configuration.</summary>
    /// <param name="client">The client used by the operation.</param>
    /// <param name="request">The request used by the operation.</param>
    /// <param name="callback">The callback used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task<Response<TResponse>> GetResponseAsync<TRequest, TResponse>(this IRequestClient<TRequest> client, TRequest request,
        RequestPipeConfiguratorCallback<TRequest> callback, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class => RequireAdvanced(client).GetResponseAsync<TResponse>(request, callback, timeout, cancellationToken);

    /// <summary>Gets either of two response types.</summary>
    /// <param name="client">The client used by the operation.</param>
    /// <param name="request">The request used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task<Response<T1, T2>> GetResponseAsync<TRequest, T1, T2>(this IRequestClient<TRequest> client, TRequest request,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where T1 : class
        where T2 : class => RequireAdvanced(client).GetResponseAsync<T1, T2>(request, timeout, cancellationToken);

    /// <summary>Gets either of two response types with advanced request-pipe configuration.</summary>
    /// <param name="client">The client used by the operation.</param>
    /// <param name="request">The request used by the operation.</param>
    /// <param name="callback">The callback used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task<Response<T1, T2>> GetResponseAsync<TRequest, T1, T2>(this IRequestClient<TRequest> client, TRequest request,
        RequestPipeConfiguratorCallback<TRequest> callback, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where T1 : class
        where T2 : class => RequireAdvanced(client).GetResponseAsync<T1, T2>(request, callback, timeout, cancellationToken);

    /// <summary>Gets any of three response types.</summary>
    /// <param name="client">The client used by the operation.</param>
    /// <param name="request">The request used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task<Response<T1, T2, T3>> GetResponseAsync<TRequest, T1, T2, T3>(this IRequestClient<TRequest> client, TRequest request,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where T1 : class
        where T2 : class
        where T3 : class => RequireAdvanced(client).GetResponseAsync<T1, T2, T3>(request, timeout, cancellationToken);

    /// <summary>Gets any of three response types with advanced request-pipe configuration.</summary>
    /// <param name="client">The client used by the operation.</param>
    /// <param name="request">The request used by the operation.</param>
    /// <param name="callback">The callback used by the operation.</param>
    /// <param name="timeout">The maximum time allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task<Response<T1, T2, T3>> GetResponseAsync<TRequest, T1, T2, T3>(this IRequestClient<TRequest> client, TRequest request,
        RequestPipeConfiguratorCallback<TRequest> callback, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where TRequest : class
        where T1 : class
        where T2 : class
        where T3 : class => RequireAdvanced(client).GetResponseAsync<T1, T2, T3>(request, callback, timeout, cancellationToken);

    internal static IAdvancedRequestClient<TRequest> RequireAdvanced<TRequest>(IRequestClient<TRequest> client)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(client);
        return client as IAdvancedRequestClient<TRequest>
            ?? throw new NotSupportedException($"The request client '{client.GetType().FullName}' does not expose advanced request operations.");
    }
}
