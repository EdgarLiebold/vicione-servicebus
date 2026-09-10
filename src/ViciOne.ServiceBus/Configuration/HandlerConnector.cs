using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects a message handler to a pipe.</summary>
/// <typeparam name="TMessage">The message contract handled by the delegate.</typeparam>
public sealed class HandlerConnector<TMessage> :
    IHandlerConnector<TMessage>
    where TMessage : class
{
    /// <summary>Connects a message-handler delegate directly to a consume pipe.</summary>
    /// <param name="consumePipe">The consume pipe that will dispatch matching messages.</param>
    /// <param name="handler">The delegate invoked for each matching message.</param>
    /// <param name="configurator">Optional middleware configuration for the handler pipe.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectHandler(IConsumePipeConnector consumePipe, MessageHandler<TMessage> handler,
        IBuildPipeConfigurator<ConsumeContext<TMessage>>? configurator)
    {
        ArgumentNullException.ThrowIfNull(consumePipe);
        ArgumentNullException.ThrowIfNull(handler);

        configurator ??= new PipeConfigurator<ConsumeContext<TMessage>>();
        configurator.AddPipeSpecification(new HandlerPipeSpecification<TMessage>(handler));

        return consumePipe.ConnectConsumePipe(configurator.Build());
    }

    /// <summary>Connects a request-scoped handler that accepts only the specified request identifier.</summary>
    /// <param name="consumePipe">The request pipe that will dispatch the matching request.</param>
    /// <param name="requestId">The request identifier that selects messages for the handler.</param>
    /// <param name="handler">The delegate invoked for the matching request.</param>
    /// <param name="configurator">Middleware configuration for the request-handler pipe.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectRequestHandler(IRequestPipeConnector consumePipe, Guid requestId, MessageHandler<TMessage> handler,
        IBuildPipeConfigurator<ConsumeContext<TMessage>> configurator)
    {
        ArgumentNullException.ThrowIfNull(consumePipe);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.AddPipeSpecification(new HandlerPipeSpecification<TMessage>(handler));

        return consumePipe.ConnectRequestPipe(requestId, configurator.Build());
    }
}
