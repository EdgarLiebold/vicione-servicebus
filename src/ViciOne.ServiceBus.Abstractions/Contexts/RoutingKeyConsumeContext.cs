namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes state for routing key consume operations.</summary>
public interface RoutingKeyConsumeContext
{
    /// <summary>The routing key for the message (defaults to "").</summary>
    string? RoutingKey { get; }
}
