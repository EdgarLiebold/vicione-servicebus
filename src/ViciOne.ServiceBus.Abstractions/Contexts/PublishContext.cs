namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for publish context.
/// </summary>
public interface PublishContext :
    SendContext
{
    /// <summary>
    /// True if the message must be delivered to a subscriber
    /// </summary>
    bool Mandatory { get; set; }
}


/// <summary>
/// Defines the contract for publish context.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface PublishContext<out T> :
    SendContext<T>,
    PublishContext
    where T : class
{
}
