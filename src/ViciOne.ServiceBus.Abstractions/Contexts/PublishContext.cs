namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes state for publish operations.</summary>
public interface PublishContext :
    SendContext
{
    /// <summary>True if the message must be delivered to a subscriber.</summary>
    bool Mandatory { get; set; }
}


/// <summary>Exposes state for publish operations.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface PublishContext<out T> :
    SendContext<T>,
    PublishContext
    where T : class
{
}
