using System;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for receive endpoint dispatcher factory.
/// </summary>
public interface IReceiveEndpointDispatcherFactory :
    IAsyncDisposable
{
    /// <summary>
    /// Creates a single receiver with all configured consumers, sagas, etc.
    /// Note that if any other receivers are created for specific consumers or sagas, those consumers and sagas will
    /// not be included in this receiver as they've already been configured.
    /// </summary>
    /// <param name="queueName"></param>
    /// <returns></returns>
    IReceiveEndpointDispatcher CreateReceiver(string queueName);

    /// <summary>
    /// Creates consumer receiver.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    IReceiveEndpointDispatcher CreateConsumerReceiver<T>(string queueName)
        where T : class, IConsumer;

    /// <summary>
    /// Creates saga receiver.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    IReceiveEndpointDispatcher CreateSagaReceiver<T>(string queueName)
        where T : class, ISaga;

    /// <summary>
    /// Creates execute activity receiver.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    IReceiveEndpointDispatcher CreateExecuteActivityReceiver<T>(string queueName)
        where T : class, IExecuteActivity;
}
