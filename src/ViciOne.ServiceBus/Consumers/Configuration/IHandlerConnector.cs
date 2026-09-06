using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects a message handler to the ConsumePipe.</summary>
/// <typeparam name="T">The message type.</typeparam>
public interface IHandlerConnector<T>
    where T : class
{
    /// <summary>Connect a message handler for all messages of type T.</summary>
    /// <param name="consumePipe">The consume pipe.</param>
    /// <param name="handler">The handler.</param>
    /// <param name="configurator">The configurator to update.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectHandler(IConsumePipeConnector consumePipe, MessageHandler<T> handler,
        IBuildPipeConfigurator<ConsumeContext<T>>? configurator);

    /// <summary>Connect a message handler for messages with the specified RequestId.</summary>
    /// <param name="consumePipe">The consume pipe.</param>
    /// <param name="requestId">The request id.</param>
    /// <param name="handler">The handler.</param>
    /// <param name="configurator">The configurator to update.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectRequestHandler(IRequestPipeConnector consumePipe, Guid requestId, MessageHandler<T> handler,
        IBuildPipeConfigurator<ConsumeContext<T>> configurator);
}
