using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Consumer;

/// <summary>
/// Retains a reference to an existing message consumer, and uses it to send consumable messages for
/// processing.
/// </summary>
/// <typeparam name="TConsumer">The consumer type.</typeparam>
public class InstanceConsumerFactory<TConsumer> :
    IConsumerFactory<TConsumer>
    where TConsumer : class
{
    readonly TConsumer _consumer;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="consumer">The consumer.</param>
    public InstanceConsumerFactory(TConsumer consumer)
    {
        _consumer = consumer;
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
