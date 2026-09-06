namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures transport.</summary>
public interface ITransportConfigurator
{
    /// <summary>Gets or sets the prefetch count.</summary>
    int PrefetchCount { set; }

    /// <summary>Gets or sets the concurrent message limit.</summary>
    int? ConcurrentMessageLimit { set; }
}
