namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures receive-transport delivery capacity.</summary>
public interface ITransportConfigurator
{
    /// <summary>Sets the broker-specific number of messages fetched ahead of processing.</summary>
    int PrefetchCount { set; }

    /// <summary>Sets the maximum number of messages delivered concurrently by the transport.</summary>
    int? ConcurrentMessageLimit { set; }
}
