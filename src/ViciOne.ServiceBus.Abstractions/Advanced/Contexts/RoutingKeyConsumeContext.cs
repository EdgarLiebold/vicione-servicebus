namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes the transport routing key associated with a received message.</summary>
public interface RoutingKeyConsumeContext
{
    /// <summary>Gets the routing key, or <see langword="null" /> when none was assigned.</summary>
    string? RoutingKey { get; }
}
