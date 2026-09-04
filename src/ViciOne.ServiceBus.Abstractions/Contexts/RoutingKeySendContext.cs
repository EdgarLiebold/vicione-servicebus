namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for routing key send context.
/// </summary>
public interface RoutingKeySendContext
{
    /// <summary>
    /// The routing key for the message (defaults to "")
    /// </summary>
    string? RoutingKey { get; set; }
}
