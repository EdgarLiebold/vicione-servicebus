using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Provides an endpoint for publish request send.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
internal sealed class PublishRequestSendEndpoint<TRequest> :
    RequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly IPublishEndpointProvider _provider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="provider">The publish endpoint provider used to resolve the request destination.</param>
    /// <param name="consumeContext">The consume context.</param>
    public PublishRequestSendEndpoint(IPublishEndpointProvider provider, ConsumeContext? consumeContext)
        : base(consumeContext)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>Gets send endpoint.</summary>
    /// <returns>A task that produces the requested value.</returns>
    protected override async Task<ISendEndpoint> GetSendEndpointAsync()
    {
        return await _provider.GetPublishSendEndpointAsync<TRequest>().ConfigureAwait(false);
    }
}
