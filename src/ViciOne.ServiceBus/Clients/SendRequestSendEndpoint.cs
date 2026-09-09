using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Sends requests for one message contract to an explicit destination.</summary>
/// <typeparam name="TRequest">The request message contract.</typeparam>
internal sealed class SendRequestSendEndpoint<TRequest> :
    RequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly Uri _destinationAddress;
    readonly ISendEndpointProvider _provider;

    /// <summary>Creates a request endpoint for an explicit destination.</summary>
    /// <param name="provider">The send endpoint provider used to resolve the request destination.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="consumeContext">The consume context whose correlation metadata is propagated, or <see langword="null" />.</param>
    public SendRequestSendEndpoint(ISendEndpointProvider provider, Uri destinationAddress, ConsumeContext? consumeContext)
        : base(consumeContext)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _destinationAddress = destinationAddress ?? throw new ArgumentNullException(nameof(destinationAddress));
    }

    /// <summary>Resolves the configured request destination.</summary>
    /// <param name="cancellationToken">Cancels endpoint resolution.</param>
    /// <returns>A task containing the resolved send endpoint.</returns>
    protected override async Task<ISendEndpoint> GetSendEndpointAsync(CancellationToken cancellationToken)
    {
        return await _provider.GetSendEndpointAsync(_destinationAddress, cancellationToken).ConfigureAwait(false);
    }
}
