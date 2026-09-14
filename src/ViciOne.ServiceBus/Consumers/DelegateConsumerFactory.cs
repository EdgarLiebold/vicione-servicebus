using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Consumers.Contexts;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Consumers;

/// <summary>Creates one consumer per delivery through a delegate and releases it after the consumer pipeline completes.</summary>
/// <typeparam name="TConsumer">The consumer implementation created for each delivery.</typeparam>
public sealed class DelegateConsumerFactory<TConsumer> :
    IConsumerFactory<TConsumer>
    where TConsumer : class
{
    readonly Func<TConsumer> _factoryMethod;

    /// <summary>Creates a consumer factory backed by the supplied delegate.</summary>
    /// <param name="factoryMethod">The delegate that creates a consumer for each delivery.</param>
    public DelegateConsumerFactory(Func<TConsumer> factoryMethod)
    {
        _factoryMethod = factoryMethod ?? throw new ArgumentNullException(nameof(factoryMethod));
    }

    /// <summary>Creates a consumer, invokes its message pipeline, and releases the consumer instance.</summary>
    /// <typeparam name="TMessage">The consumed message contract.</typeparam>
    /// <param name="context">The received message and its consume context.</param>
    /// <param name="next">The consumer pipeline to invoke with the created instance.</param>
    /// <returns>A task that completes after the consumer pipeline and consumer lifetime have finished.</returns>
    public async Task SendAsync<TMessage>(ConsumeContext<TMessage> context, IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        TConsumer? consumer = null;
        Exception? operationFailure = null;
        try
        {
            consumer = _factoryMethod();
            if (consumer == null)
                throw new ConsumerException($"Unable to resolve consumer type '{TypeCache<TConsumer>.ShortName}'.");

            await next.SendAsync(new ConsumerConsumeContextScope<TConsumer, TMessage>(context, consumer)).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            operationFailure = exception;
        }

        await OwnedConsumerLifetime.ReleaseAfterOperationAsync(consumer, operationFailure).ConfigureAwait(false);
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateConsumerFactoryScope<TConsumer>("delegate");
    }
}
