namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Exposes Amazon SQS-native send settings for a typed message.</summary>
/// <typeparam name="T">The message type.</typeparam>
public interface AmazonSqsSendContext<out T> :
    AmazonSqsSendContext,
    SendContext<T>
    where T : class
{
}


/// <summary>Exposes Amazon SQS-native send settings.</summary>
public interface AmazonSqsSendContext :
    SendContext
{
    /// <summary>Sets the FIFO message-group identifier.</summary>
    string? GroupId { set; }
    /// <summary>Sets the FIFO message-deduplication identifier.</summary>
    string? DeduplicationId { set; }
    /// <summary>Sets the per-message delivery delay in whole seconds.</summary>
    int? DelaySeconds { set; }
}
