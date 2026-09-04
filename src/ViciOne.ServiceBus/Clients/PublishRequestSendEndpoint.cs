using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients;

/// <summary>
/// Provides a publish request send endpoint implementation.
/// </summary>
/// <typeparam name="TRequest">The t request type.</typeparam>
public class PublishRequestSendEndpoint<TRequest> :
    RequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly IPublishEndpointProvider _provider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <param name="consumeContext">The consume context value.</param>
    public PublishRequestSendEndpoint(IPublishEndpointProvider provider, ConsumeContext? consumeContext)
        : base(consumeContext)
    {
        _provider = provider;
    }

    /// <summary>
    /// Gets send endpoint.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override async Task<ISendEndpoint> GetSendEndpointAsync()
    {
        return await _provider.GetPublishSendEndpointAsync<TRequest>().ConfigureAwait(false);
    }
}
