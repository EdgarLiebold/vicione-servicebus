namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Defines the contract for notification context.
/// </summary>
public interface NotificationContext :
    PipeContext
{
    /// <summary>
    /// Connects notification sink.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="listener">The listener value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectNotificationSink(string queueName, IQueueNotificationListener listener);
}
