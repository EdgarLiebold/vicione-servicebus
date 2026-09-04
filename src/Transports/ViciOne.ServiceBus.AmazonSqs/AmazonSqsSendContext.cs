namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for amazon sqs send context.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface AmazonSqsSendContext<out T> :
    AmazonSqsSendContext,
    SendContext<T>
    where T : class
{
}


/// <summary>
/// Defines the contract for amazon sqs send context.
/// </summary>
public interface AmazonSqsSendContext :
    SendContext
{
    /// <summary>
    /// Gets or sets the group id value.
    /// </summary>
    string? GroupId { set; }
    /// <summary>
    /// Gets or sets the deduplication id value.
    /// </summary>
    string? DeduplicationId { set; }
    /// <summary>
    /// Gets or sets the delay seconds value.
    /// </summary>
    int? DelaySeconds { set; }
}
