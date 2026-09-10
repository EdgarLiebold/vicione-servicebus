namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Identifies the kind of SQL transport destination.</summary>
public enum SqlEndpointKind
{
    /// <summary>A point-to-point queue destination.</summary>
    Queue = 0,

    /// <summary>A publish-subscribe topic destination.</summary>
    Topic = 1,
}
