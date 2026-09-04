namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>
/// Defines the contract for consume pipe connector.
/// </summary>
public interface IConsumePipeConnector
{
    /// <summary>
    /// Connects consume pipe.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class;

    /// <summary>
    /// Connects consume pipe.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class;
}
