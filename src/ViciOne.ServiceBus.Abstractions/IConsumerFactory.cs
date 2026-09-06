using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Maps an instance of a consumer to one or more Consume methods for the specified message type
/// The whole purpose for this interface is to allow the creator of the consumer to manage the lifecycle
/// of the consumer, along with anything else that needs to be managed by the factory, container, etc.
/// </summary>
/// <typeparam name="TConsumer">The Consumer type.</typeparam>
public interface IConsumerFactory<out TConsumer> :
    IProbeSite
    where TConsumer : class
{
    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendAsync<T>(ConsumeContext<T> context, IPipe<ConsumerConsumeContext<TConsumer, T>> next)
        where T : class;
}
