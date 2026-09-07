using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>Consumes messages together with their receive context and transport metadata.</summary>
/// <typeparam name="TMessage">The message contract type.</typeparam>
public interface IConsumer<in TMessage> :
    IConsumer
    where TMessage : class
{
    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The received message and its consume context.</param>
    /// <returns>A task that completes when message consumption has finished.</returns>
    Task ConsumeAsync(ConsumeContext<TMessage> context);
}

/// <summary>Identifies message-consumer implementations for registration and discovery.</summary>
public interface IConsumer
{
}
