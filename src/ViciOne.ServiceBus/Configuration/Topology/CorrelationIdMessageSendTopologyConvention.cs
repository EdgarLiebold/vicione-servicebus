using System.Collections.Generic;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Selects and applies correlation identifiers for one sent message contract.</summary>
/// <typeparam name="TMessage">The sent message contract type.</typeparam>
sealed class CorrelationIdMessageSendTopologyConvention<TMessage> :
    ICorrelationIdMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    readonly List<ICorrelationIdSelector<TMessage>> _selectors;

    /// <summary>Initializes the convention with the standard correlation sources in precedence order.</summary>
    public CorrelationIdMessageSendTopologyConvention()
    {
        _selectors =
        [
            new CorrelatedByCorrelationIdSelector<TMessage>(),
            new PropertyCorrelationIdSelector<TMessage>("CorrelationId"),
            new PropertyCorrelationIdSelector<TMessage>("EventId"),
            new PropertyCorrelationIdSelector<TMessage>("CommandId")
        ];
    }

    bool IMessageSendTopologyConvention.TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
    {
        convention = this as IMessageSendTopologyConvention<T>;

        return convention != null;
    }

    bool IMessageSendTopologyConvention<TMessage>.TryGetMessageSendTopology(
        [NotNullWhen(true)] out IMessageSendTopology<TMessage>? messageSendTopology)
    {
        if (TryGetCorrelationIdResolver(out IMessageCorrelationId<TMessage>? messageCorrelationId))
        {
            messageSendTopology = new SetCorrelationIdMessageSendTopology<TMessage>(messageCorrelationId);
            return true;
        }

        messageSendTopology = null;
        return false;
    }

    /// <summary>Sets the correlation resolver with precedence over inferred sources.</summary>
    /// <param name="messageCorrelationId">The correlation resolver.</param>
    public void SetCorrelationId(IMessageCorrelationId<TMessage> messageCorrelationId)
    {
        ArgumentNullException.ThrowIfNull(messageCorrelationId);

        _selectors.Insert(0, new SetCorrelationIdSelector<TMessage>(messageCorrelationId));
    }

    /// <summary>Attempts to get the first correlation resolver that supports the message contract.</summary>
    /// <param name="messageCorrelationId">Receives the selected correlation resolver.</param>
    /// <returns><see langword="true" /> when a resolver is available; otherwise, <see langword="false" />.</returns>
    public bool TryGetCorrelationIdResolver([NotNullWhen(true)] out IMessageCorrelationId<TMessage>? messageCorrelationId)
    {
        for (var index = 0; index < _selectors.Count; index++)
        {
            if (_selectors[index].TryGetCorrelationIdResolver(out messageCorrelationId))
                return true;
        }

        messageCorrelationId = null;
        return false;
    }
}
