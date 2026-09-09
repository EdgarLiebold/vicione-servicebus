namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides transport routing-key metadata for an outgoing message.</summary>
public interface RoutingKeySendContext
{
    /// <summary>Gets or sets the routing key, or <see langword="null" /> when none is assigned.</summary>
    string? RoutingKey { get; set; }
}
