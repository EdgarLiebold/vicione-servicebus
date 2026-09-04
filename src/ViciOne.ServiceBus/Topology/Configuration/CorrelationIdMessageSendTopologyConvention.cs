using System.Collections.Generic;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a correlation id message send topology convention implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class CorrelationIdMessageSendTopologyConvention<TMessage> :
    ICorrelationIdMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    readonly List<ICorrelationIdSelector<TMessage>> _selectors;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
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
        if (TryGetMessageCorrelationId(out IMessageCorrelationId<TMessage>? messageCorrelationId))
        {
            messageSendTopology = new SetCorrelationIdMessageSendTopology<TMessage>(messageCorrelationId);
            return true;
        }

        messageSendTopology = null;
        return false;
    }

    /// <summary>
    /// Sets correlation id.
    /// </summary>
    /// <param name="messageCorrelationId">The message correlation id value.</param>
    public void SetCorrelationId(IMessageCorrelationId<TMessage> messageCorrelationId)
    {
        _selectors.Insert(0, new SetCorrelationIdSelector<TMessage>(messageCorrelationId));
    }

    /// <summary>
    /// Attempts to get message correlation id.
    /// </summary>
    /// <param name="messageCorrelationId">The message correlation id value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessageCorrelationId([NotNullWhen(true)] out IMessageCorrelationId<TMessage>? messageCorrelationId)
    {
        for (var index = 0; index < _selectors.Count; index++)
        {
            if (_selectors[index].TryGetSetCorrelationId(out messageCorrelationId))
                return true;
        }

        messageCorrelationId = null;
        return false;
    }
}
