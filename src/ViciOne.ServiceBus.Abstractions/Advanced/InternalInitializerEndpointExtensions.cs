using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Initializers;

internal static class InternalInitializerEndpointExtensions
{
    internal static RequestHandle<TRequest> Create<TRequest>(this IRequestClient<TRequest> client, object values,
        CancellationToken cancellationToken = default, RequestTimeout timeout = default)
        where TRequest : class => AdvancedRequestClientExtensions.RequireAdvanced(client).Create(values, cancellationToken, timeout);

    internal static Task SendAsync<T>(this ISendEndpoint endpoint, object values,
        CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).SendAsync<T>(values, cancellationToken);

    internal static Task SendAsync<T>(this ISendEndpoint endpoint, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).SendAsync(values, pipe, cancellationToken);

    internal static Task SendAsync<T>(this ISendEndpoint endpoint, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).SendAsync<T>(values, pipe, cancellationToken);

    internal static Task PublishAsync<T>(this IPublishEndpoint endpoint, object values,
        CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).PublishAsync<T>(values, cancellationToken);

    internal static Task PublishAsync<T>(this IPublishEndpoint endpoint, object values, IPipe<PublishContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).PublishAsync(values, pipe, cancellationToken);

    internal static Task PublishAsync<T>(this IPublishEndpoint endpoint, object values, IPipe<PublishContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).PublishAsync<T>(values, pipe, cancellationToken);

    static IAdvancedSendEndpoint RequireAdvanced(ISendEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        return endpoint as IAdvancedSendEndpoint
            ?? throw new NotSupportedException($"The send endpoint '{endpoint.GetType().FullName}' does not expose initializer operations.");
    }

    static IAdvancedPublishEndpoint RequireAdvanced(IPublishEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        return endpoint as IAdvancedPublishEndpoint
            ?? throw new NotSupportedException($"The publish endpoint '{endpoint.GetType().FullName}' does not expose initializer operations.");
    }
}
