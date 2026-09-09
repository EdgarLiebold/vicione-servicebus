namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Exposes a message correlation key to the core send-topology pipeline.</summary>
/// <typeparam name="TKey">The type of correlation identifier carried by the message.</typeparam>
[MessageContractExclusion]
public interface IMessageCorrelation<out TKey>
{
    /// <summary>Gets the identifier used to correlate the message.</summary>
    TKey CorrelationId { get; }
}
