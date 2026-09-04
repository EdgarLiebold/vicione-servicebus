using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Consumer;

/// <summary>
/// Retains a reference to an existing message consumer, and uses it to send consumable messages for
/// processing.
/// </summary>
/// <typeparam name="TConsumer">The consumer type</typeparam>
public class InstanceConsumerFactory<TConsumer> :
    IConsumerFactory<TConsumer>
    where TConsumer : class
{
    readonly TConsumer _consumer;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="consumer">The consumer value.</param>
    public InstanceConsumerFactory(TConsumer consumer)
    {
        _consumer = consumer;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<TMessage>(ConsumeContext<TMessage> context, IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
        where TMessage : class
    {
        return next.SendAsync(new ConsumerConsumeContextScope<TConsumer, TMessage>(context, _consumer));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateConsumerFactoryScope<TConsumer>("instance");
    }
}
