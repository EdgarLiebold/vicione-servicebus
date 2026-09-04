namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for routing key consume context.
/// </summary>
public interface RoutingKeyConsumeContext
{
    /// <summary>
    /// The routing key for the message (defaults to "")
    /// </summary>
    string? RoutingKey { get; }
}
