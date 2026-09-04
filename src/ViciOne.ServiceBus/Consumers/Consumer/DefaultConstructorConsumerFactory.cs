using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Consumer;

/// <summary>
/// Provides a default constructor consumer factory implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public class DefaultConstructorConsumerFactory<TConsumer> :
    IConsumerFactory<TConsumer>
    where TConsumer : class, new()
{
    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync<T>(ConsumeContext<T> context, IPipe<ConsumerConsumeContext<TConsumer, T>> next)
        where T : class
    {
        TConsumer? consumer = null;
        try
        {
            consumer = new TConsumer();

            await next.SendAsync(new ConsumerConsumeContextScope<TConsumer, T>(context, consumer)).ConfigureAwait(false);
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
