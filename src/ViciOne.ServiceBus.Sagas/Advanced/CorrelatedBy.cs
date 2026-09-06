namespace ViciOne.ServiceBus.Advanced;

/// <summary>Marks a message contract that exposes its own correlation identifier.</summary>
/// <typeparam name="TKey">The correlation identifier type.</typeparam>
[MessageContractExclusion]
public interface CorrelatedBy<out TKey> :
    global::ViciOne.ServiceBus.Advanced.Topology.IMessageCorrelation<TKey>
{
    /// <summary>Gets the identifier used to correlate the message.</summary>
    new TKey CorrelationId { get; }
}
