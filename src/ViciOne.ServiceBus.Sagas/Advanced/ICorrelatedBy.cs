namespace ViciOne.ServiceBus.Advanced;

/// <summary>Identifies a message contract that carries its correlation key in the message body.</summary>
/// <typeparam name="TKey">The type of correlation key carried by the message.</typeparam>
[MessageContractExclusion]
public interface ICorrelatedBy<out TKey> :
    global::ViciOne.ServiceBus.Advanced.Topology.IMessageCorrelation<TKey>
{
    /// <summary>Gets the key used to correlate this message with its owner.</summary>
    new TKey CorrelationId { get; }
}
