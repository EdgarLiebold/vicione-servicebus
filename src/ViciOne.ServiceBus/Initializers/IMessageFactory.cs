// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Initializers
{
    /// <summary>
    /// Creates the message type
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    public interface IMessageFactory<out TMessage>
        where TMessage : class
    {
        InitializeContext<TMessage> Create(InitializeContext context);
    }


    /// <summary>
    /// Creates the message type
    /// </summary>
    public interface IMessageFactory
    {
        object Create();
    }
}
