using System;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes consume context output message type pipeline stages.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IConsumeContextOutputMessageTypeFilter<out TMessage> :
    IFilter<ConsumeContext>,
    IPipeConnector<ConsumeContext<TMessage>>,
    IConsumeMessageObserverConnector<TMessage>
    where TMessage : class
{
    /// <summary>Connects pipe.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectPipe(Guid key, IPipe<ConsumeContext<TMessage>> pipe);
}
