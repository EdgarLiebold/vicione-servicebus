namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Exposes state for notification operations.</summary>
public interface NotificationContext :
    PipeContext
{
    /// <summary>Connects notification sink.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="listener">The listener.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectNotificationSink(string queueName, IQueueNotificationListener listener);
}
