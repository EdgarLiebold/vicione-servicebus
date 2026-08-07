// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Mediator
{
    /// <summary>
    /// A mediator interface, signaling the response type expected by a request
    /// </summary>
    /// <typeparam name="TResponse">The response type tied to the request</typeparam>
    [ExcludeFromTopology]
    [ExcludeFromImplementedTypes]
    public interface Request<out TResponse>
        where TResponse : class
    {
    }
}
