using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Consumers;

/// <summary>Adapts an untyped object factory into a typed, per-delivery consumer factory.</summary>
/// <typeparam name="TConsumer">The consumer implementation requested from the object factory.</typeparam>
public sealed class ObjectConsumerFactory<TConsumer> :
    IConsumerFactory<TConsumer>
    where TConsumer : class
{
    readonly IConsumerFactory<TConsumer> _delegate;

    /// <summary>Creates a typed consumer factory over the supplied object factory.</summary>
    /// <param name="objectFactory">The factory invoked with the requested consumer type for each delivery.</param>
    public ObjectConsumerFactory(Func<Type, object> objectFactory)
    {
        ArgumentNullException.ThrowIfNull(objectFactory);
        _delegate = new DelegateConsumerFactory<TConsumer>(() => CreateConsumer(objectFactory));
    }

    /// <summary>Invokes the message pipeline with a consumer created by the object factory.</summary>
    /// <typeparam name="TMessage">The consumed message contract.</typeparam>
    /// <param name="context">The received message and its consume context.</param>
    /// <param name="next">The consumer pipeline to invoke with the created instance.</param>
    /// <returns>A task that completes after the consumer pipeline and consumer lifetime have finished.</returns>
    public Task SendAsync<TMessage>(ConsumeContext<TMessage> context, IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        return _delegate.SendAsync(context, next);
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateConsumerFactoryScope<TConsumer>("objectFactory");
    }

    static TConsumer CreateConsumer(Func<Type, object> objectFactory)
    {
        object? value = objectFactory(typeof(TConsumer));
        if (value is TConsumer consumer)
            return consumer;

        string actualType = value is null ? "null" : TypeCache.GetShortName(value.GetType());
        throw new ConsumerException(
            $"The object factory returned '{actualType}' when consumer type '{TypeCache<TConsumer>.ShortName}' was requested.");
    }
}
