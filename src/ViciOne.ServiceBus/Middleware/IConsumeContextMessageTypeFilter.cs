using System;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Defines the contract for consume context message type filter.
/// </summary>
public interface IConsumeContextMessageTypeFilter :
    IFilter<ConsumeContext>,
    IConsumeMessageObserverConnector,
    IConsumeObserverConnector
{
    /// <summary>
    /// Connects message pipe.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectMessagePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class;

    /// <summary>
    /// Connects message pipe.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectMessagePipe<T>(Guid key, IPipe<ConsumeContext<T>> pipe)
        where T : class;
}
