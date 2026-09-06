using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Represents an asynchronous message-consumption handler.</summary>
/// <typeparam name="TMessage">The message contract type.</typeparam>
/// <param name="context">The received message and its consume context.</param>
/// <returns>A task that completes when message consumption has finished.</returns>
public delegate Task MessageHandler<in TMessage>(ConsumeContext<TMessage> context)
    where TMessage : class;
