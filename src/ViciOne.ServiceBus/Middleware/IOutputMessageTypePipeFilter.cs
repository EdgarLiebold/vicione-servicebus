using System;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Defines the contract for consume context output message type filter.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IConsumeContextOutputMessageTypeFilter<out TMessage> :
    IFilter<ConsumeContext>,
    IPipeConnector<ConsumeContext<TMessage>>,
    IConsumeMessageObserverConnector<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Connects pipe.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectPipe(Guid key, IPipe<ConsumeContext<TMessage>> pipe);
}
