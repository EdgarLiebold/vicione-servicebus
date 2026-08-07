// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.Threading.Tasks;


    /// <summary>
    /// A message handler is a delegate type that asynchronously consumes the message
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="context">The consume context</param>
    /// <returns>An awaitable task that is completed once the message has been consumed</returns>
    public delegate Task MessageHandler<in TMessage>(ConsumeContext<TMessage> context)
        where TMessage : class;
}
