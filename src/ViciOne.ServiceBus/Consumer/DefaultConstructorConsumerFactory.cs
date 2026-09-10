using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Consumer;

/// <summary>Creates one consumer per delivery through its parameterless constructor and releases it after the consumer pipeline completes.</summary>
/// <typeparam name="TConsumer">The consumer implementation created for each delivery.</typeparam>
public sealed class DefaultConstructorConsumerFactory<TConsumer> :
    IConsumerFactory<TConsumer>
    where TConsumer : class, new()
{
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
        try
        {
            consumer = new TConsumer();

            await next.SendAsync(new ConsumerConsumeContextScope<TConsumer, TMessage>(context, consumer)).ConfigureAwait(false);
        }
        finally
        {
            switch (consumer)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateConsumerFactoryScope<TConsumer>("defaultConstructor");
    }
}
