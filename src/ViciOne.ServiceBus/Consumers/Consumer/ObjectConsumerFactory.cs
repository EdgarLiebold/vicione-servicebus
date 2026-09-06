using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Consumer;

/// <summary>Creates object consumer instances.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public class ObjectConsumerFactory<TConsumer> :
    IConsumerFactory<TConsumer>
    where TConsumer : class
{
    readonly IConsumerFactory<TConsumer> _delegate;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="objectFactory">The object factory.</param>
    public ObjectConsumerFactory(Func<Type, object> objectFactory)
    {
        _delegate = new DelegateConsumerFactory<TConsumer>(() => (TConsumer)objectFactory(typeof(TConsumer)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync<TMessage>(ConsumeContext<TMessage> context, IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
        where TMessage : class
    {
        return _delegate.SendAsync(context, next);
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateConsumerFactoryScope<TConsumer>("objectFactory");
    }
}
