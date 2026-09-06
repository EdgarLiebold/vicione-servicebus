namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes state for routing key send operations.</summary>
public interface RoutingKeySendContext
{
    /// <summary>The routing key for the message (defaults to "").</summary>
    string? RoutingKey { get; set; }
}
