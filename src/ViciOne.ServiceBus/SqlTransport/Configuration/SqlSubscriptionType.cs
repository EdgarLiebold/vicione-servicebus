namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Specifies the available sql subscription type values.
/// </summary>
public enum SqlSubscriptionType
{
    /// <summary>
    /// Indicates all.
    /// </summary>
    All = 1,
    /// <summary>
    /// Indicates routing key.
    /// </summary>
    RoutingKey = 2,
    /// <summary>
    /// Indicates pattern.
    /// </summary>
    Pattern = 3
}
