using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects a message handler to a pipe.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class HandlerConnector<TMessage> :
    IHandlerConnector<TMessage>
    where TMessage : class
{
    /// <summary>Connects handler.</summary>
    /// <param name="consumePipe">The consume pipe.</param>
    /// <param name="handler">The handler.</param>
    /// <param name="configurator">The configurator to update.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectHandler(IConsumePipeConnector consumePipe, MessageHandler<TMessage> handler,
        IBuildPipeConfigurator<ConsumeContext<TMessage>>? configurator)
    {
        configurator ??= new PipeConfigurator<ConsumeContext<TMessage>>();
        configurator.AddPipeSpecification(new HandlerPipeSpecification<TMessage>(handler));

        return consumePipe.ConnectConsumePipe(configurator.Build());
    }

    /// <summary>Connects request handler.</summary>
    /// <param name="consumePipe">The consume pipe.</param>
    /// <param name="requestId">The request id.</param>
    /// <param name="handler">The handler.</param>
    /// <param name="configurator">The configurator to update.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectRequestHandler(IRequestPipeConnector consumePipe, Guid requestId, MessageHandler<TMessage> handler,
        IBuildPipeConfigurator<ConsumeContext<TMessage>> configurator)
    {
        configurator.AddPipeSpecification(new HandlerPipeSpecification<TMessage>(handler));

        return consumePipe.ConnectRequestPipe(requestId, configurator.Build());
    }
}
