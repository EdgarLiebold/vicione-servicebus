using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Creates and owns consumer instances while invoking their message pipeline.</summary>
/// <typeparam name="TConsumer">The consumer implementation type.</typeparam>
public interface IConsumerFactory<out TConsumer> :
    IProbeSite
    where TConsumer : class
{
    /// <summary>Invokes a consumer instance and its downstream consume pipeline.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="context">The received message and its consume context.</param>
    /// <param name="next">The consumer pipeline to invoke with the owned instance.</param>
    /// <returns>A task that completes after the consumer pipeline and owned lifetime have finished.</returns>
    Task SendAsync<T>(ConsumeContext<T> context, IPipe<ConsumerConsumeContext<TConsumer, T>> next)
        where T : class;
}
