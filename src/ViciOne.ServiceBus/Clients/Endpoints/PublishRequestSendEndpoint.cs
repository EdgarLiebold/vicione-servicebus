using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients.Endpoints;

/// <summary>Publishes requests for one message contract.</summary>
/// <typeparam name="TRequest">The request message contract.</typeparam>
internal sealed class PublishRequestSendEndpoint<TRequest> :
    RequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly IPublishEndpointProvider _provider;

    /// <summary>Creates a publish-backed request endpoint.</summary>
    /// <param name="provider">The publish endpoint provider used to resolve the request destination.</param>
    /// <param name="consumeContext">The consume context whose correlation metadata is propagated, or <see langword="null" />.</param>
    public PublishRequestSendEndpoint(IPublishEndpointProvider provider, ConsumeContext? consumeContext)
        : base(consumeContext)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>Resolves the publish endpoint for the request contract.</summary>
    /// <param name="cancellationToken">Cancels endpoint resolution.</param>
    /// <returns>A task containing the publish send endpoint.</returns>
    protected override Task<ISendEndpoint> GetSendEndpointAsync(CancellationToken cancellationToken) =>
        _provider.GetPublishSendEndpointAsync<TRequest>(cancellationToken);
}
