using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Provides an endpoint for send request send.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
internal sealed class SendRequestSendEndpoint<TRequest> :
    RequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly Uri _destinationAddress;
    readonly ISendEndpointProvider _provider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="provider">The send endpoint provider used to resolve the request destination.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="consumeContext">The consume context.</param>
    public SendRequestSendEndpoint(ISendEndpointProvider provider, Uri destinationAddress, ConsumeContext? consumeContext)
        : base(consumeContext)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _destinationAddress = destinationAddress ?? throw new ArgumentNullException(nameof(destinationAddress));
    }

    /// <summary>Gets send endpoint.</summary>
    /// <returns>A task that produces the requested value.</returns>
    protected override async Task<ISendEndpoint> GetSendEndpointAsync()
    {
        return await _provider.GetSendEndpointAsync(_destinationAddress).ConfigureAwait(false);
    }
}
