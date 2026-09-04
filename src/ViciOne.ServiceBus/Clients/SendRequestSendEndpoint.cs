using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients;

/// <summary>
/// Provides a send request send endpoint implementation.
/// </summary>
/// <typeparam name="TRequest">The t request type.</typeparam>
public class SendRequestSendEndpoint<TRequest> :
    RequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly Uri _destinationAddress;
    readonly ISendEndpointProvider _provider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="consumeContext">The consume context value.</param>
    public SendRequestSendEndpoint(ISendEndpointProvider provider, Uri destinationAddress, ConsumeContext? consumeContext)
        : base(consumeContext)
    {
        _provider = provider;
        _destinationAddress = destinationAddress;
    }

    /// <summary>
    /// Gets send endpoint.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override async Task<ISendEndpoint> GetSendEndpointAsync()
    {
        return await _provider.GetSendEndpointAsync(_destinationAddress).ConfigureAwait(false);
    }
}
