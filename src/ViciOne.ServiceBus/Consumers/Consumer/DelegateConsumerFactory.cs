using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Consumer;

/// <summary>
/// Provides a delegate consumer factory implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public class DelegateConsumerFactory<TConsumer> :
    IConsumerFactory<TConsumer>
    where TConsumer : class
{
    readonly Func<TConsumer> _factoryMethod;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="factoryMethod">The factory method value.</param>
    public DelegateConsumerFactory(Func<TConsumer> factoryMethod)
    {
        _factoryMethod = factoryMethod ?? throw new ArgumentNullException(nameof(factoryMethod));
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync<TMessage>(ConsumeContext<TMessage> context, IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
        where TMessage : class
    {
        TConsumer? consumer = null;
        try
        {
            consumer = _factoryMethod();
            if (consumer == null)
                throw new ConsumerException($"Unable to resolve consumer type '{TypeCache<TConsumer>.ShortName}'.");

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
        context.CreateConsumerFactoryScope<TConsumer>("delegate");
    }
}
