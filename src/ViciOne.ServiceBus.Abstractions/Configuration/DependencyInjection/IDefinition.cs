namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by definition.</summary>
public interface IDefinition
{
    /// <summary>Gets the concurrent message limit.</summary>
    int? ConcurrentMessageLimit { get; }
}
