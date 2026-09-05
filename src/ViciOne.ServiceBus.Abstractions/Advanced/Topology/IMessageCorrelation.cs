namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>
/// Exposes a message correlation key to the core send-topology pipeline.
/// </summary>
/// <typeparam name="TKey">The correlation-key type.</typeparam>
[MessageContractExclusion]
public interface IMessageCorrelation<out TKey>
{
    /// <summary>
    /// Gets the correlation key.
    /// </summary>
    TKey CorrelationId { get; }
}
