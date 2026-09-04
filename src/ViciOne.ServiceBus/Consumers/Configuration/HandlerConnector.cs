using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Connects a message handler to a pipe
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public class HandlerConnector<TMessage> :
    IHandlerConnector<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Connects handler.
    /// </summary>
    /// <param name="consumePipe">The consume pipe value.</param>
    /// <param name="handler">The handler value.</param>
    /// <param name="configurator">The configurator value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectHandler(IConsumePipeConnector consumePipe, MessageHandler<TMessage> handler,
        IBuildPipeConfigurator<ConsumeContext<TMessage>>? configurator)
    {
        configurator ??= new PipeConfigurator<ConsumeContext<TMessage>>();
        configurator.AddPipeSpecification(new HandlerPipeSpecification<TMessage>(handler));

        return consumePipe.ConnectConsumePipe(configurator.Build());
    }

    /// <summary>
    /// Connects request handler.
    /// </summary>
    /// <param name="consumePipe">The consume pipe value.</param>
    /// <param name="requestId">The request id value.</param>
    /// <param name="handler">The handler value.</param>
    /// <param name="configurator">The configurator value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectRequestHandler(IRequestPipeConnector consumePipe, Guid requestId, MessageHandler<TMessage> handler,
        IBuildPipeConfigurator<ConsumeContext<TMessage>> configurator)
    {
        configurator.AddPipeSpecification(new HandlerPipeSpecification<TMessage>(handler));

        return consumePipe.ConnectRequestPipe(requestId, configurator.Build());
    }
}
