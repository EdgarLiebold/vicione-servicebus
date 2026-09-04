using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Consumer;

/// <summary>
/// Provides an object consumer factory implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public class ObjectConsumerFactory<TConsumer> :
    IConsumerFactory<TConsumer>
    where TConsumer : class
{
    readonly IConsumerFactory<TConsumer> _delegate;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="objectFactory">The object factory value.</param>
    public ObjectConsumerFactory(Func<Type, object> objectFactory)
    {
        _delegate = new DelegateConsumerFactory<TConsumer>(() => (TConsumer)objectFactory(typeof(TConsumer)));
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
        return _delegate.SendAsync(context, next);
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateConsumerFactoryScope<TConsumer>("objectFactory");
    }
}
