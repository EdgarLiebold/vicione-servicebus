using System;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes consume context message type pipeline stages.</summary>
public interface IConsumeContextMessageTypeFilter :
    IFilter<ConsumeContext>,
    IConsumeMessageObserverConnector,
    IConsumeObserverConnector
{
    /// <summary>Connects message pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectMessagePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class;

    /// <summary>Connects message pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectMessagePipe<T>(Guid key, IPipe<ConsumeContext<T>> pipe)
        where T : class;
}
