using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects message-handler delegates to consume and request pipes.</summary>
/// <typeparam name="TMessage">The message contract handled by the delegate.</typeparam>
public interface IHandlerConnector<TMessage>
    where TMessage : class
{
    /// <summary>Connects a handler for every message of the configured contract.</summary>
    /// <param name="consumePipe">The consume pipe that will dispatch matching messages.</param>
    /// <param name="handler">The delegate invoked for each matching message.</param>
    /// <param name="configurator">Optional middleware configuration for the handler pipe.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectHandler(IConsumePipeConnector consumePipe, MessageHandler<TMessage> handler,
        IBuildPipeConfigurator<ConsumeContext<TMessage>>? configurator);

    /// <summary>Connects a handler for messages with the specified request identifier.</summary>
    /// <param name="consumePipe">The request pipe that will dispatch the matching request.</param>
    /// <param name="requestId">The request identifier that selects messages for the handler.</param>
    /// <param name="handler">The delegate invoked for the matching request.</param>
    /// <param name="configurator">Middleware configuration for the request-handler pipe.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectRequestHandler(IRequestPipeConnector consumePipe, Guid requestId, MessageHandler<TMessage> handler,
        IBuildPipeConfigurator<ConsumeContext<TMessage>> configurator);
}
