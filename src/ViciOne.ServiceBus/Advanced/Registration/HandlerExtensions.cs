using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Registers message-handler delegates with receive endpoints and consume pipes.</summary>
public static class HandlerExtensions
{
    /// <summary>Registers a message-handler delegate with a receive endpoint.</summary>
    /// <typeparam name="TMessage">The message contract handled by the delegate.</typeparam>
    /// <param name="configurator">The receive endpoint that will host the handler.</param>
    /// <param name="handler">The delegate invoked for each matching message.</param>
    /// <param name="configure">An optional callback that configures the handler pipeline.</param>
    public static void Handler<TMessage>(this IReceiveEndpointConfigurator configurator, MessageHandler<TMessage> handler,
        Action<IHandlerConfigurator<TMessage>>? configure = null)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(handler);

        var handlerConfigurator = new HandlerConfigurator<TMessage>(handler, configurator);

        configure?.Invoke(handlerConfigurator);

        configurator.AddEndpointSpecification(handlerConfigurator);
    }

    /// <summary>Connects a message-handler delegate directly to a consume pipe.</summary>
    /// <typeparam name="TMessage">The message contract handled by the delegate.</typeparam>
    /// <param name="connector">The consume pipe that will dispatch matching messages.</param>
    /// <param name="handler">The delegate invoked for each matching message.</param>
    /// <param name="configurator">Optional middleware configuration for the handler pipe.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public static ConnectHandle ConnectHandler<TMessage>(this IConsumePipeConnector connector, MessageHandler<TMessage> handler,
        IBuildPipeConfigurator<ConsumeContext<TMessage>>? configurator = null)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(connector);
        ArgumentNullException.ThrowIfNull(handler);

        return HandlerConnectorCache<TMessage>.Connector.ConnectHandler(connector, handler, configurator);
    }

    /// <summary>Connects a request-scoped handler that accepts only the specified request identifier.</summary>
    /// <typeparam name="TMessage">The request message contract handled by the delegate.</typeparam>
    /// <param name="connector">The request pipe that will dispatch the matching request.</param>
    /// <param name="requestId">The request identifier that selects messages for the handler.</param>
    /// <param name="handler">The delegate invoked for the matching request.</param>
    /// <param name="configurator">Middleware configuration for the request-handler pipe.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public static ConnectHandle ConnectRequestHandler<TMessage>(this IRequestPipeConnector connector, Guid requestId,
        MessageHandler<TMessage> handler, IBuildPipeConfigurator<ConsumeContext<TMessage>> configurator)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(connector);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(configurator);

        return HandlerConnectorCache<TMessage>.Connector.ConnectRequestHandler(connector, requestId, handler, configurator);
    }
}
