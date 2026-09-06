namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines transport configuration.</summary>
public interface ITransportConfiguration :
    ISpecification
{
    /// <summary>Gets the configurator.</summary>
    ITransportConfigurator Configurator { get; }

    /// <summary>Gets the prefetch count.</summary>
    int PrefetchCount { get; }

    /// <summary>Gets the concurrent message limit.</summary>
    int? ConcurrentMessageLimit { get; }

    /// <summary>Gets concurrent message limit.</summary>
    /// <returns>The concurrent message limit.</returns>
    int GetConcurrentMessageLimit();
}
