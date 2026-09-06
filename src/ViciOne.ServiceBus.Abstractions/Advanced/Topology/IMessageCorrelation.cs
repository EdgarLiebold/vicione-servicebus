namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Exposes a message correlation key to the core send-topology pipeline.</summary>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
[MessageContractExclusion]
public interface IMessageCorrelation<out TKey>
{
    /// <summary>Gets the correlation id.</summary>
    TKey CorrelationId { get; }
}
