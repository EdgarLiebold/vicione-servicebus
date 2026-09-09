namespace ViciOne.ServiceBus.Configuration;

/// <summary>Exposes concurrency configuration shared by registered component definitions.</summary>
public interface IDefinition
{
    /// <summary>Gets the maximum number of messages the component may process concurrently.</summary>
    int? ConcurrentMessageLimit { get; }
}
