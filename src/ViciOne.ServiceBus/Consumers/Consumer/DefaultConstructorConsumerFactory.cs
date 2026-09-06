using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Consumer;

/// <summary>Creates default constructor consumer instances.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public class DefaultConstructorConsumerFactory<TConsumer> :
    IConsumerFactory<TConsumer>
    where TConsumer : class, new()
{
    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
