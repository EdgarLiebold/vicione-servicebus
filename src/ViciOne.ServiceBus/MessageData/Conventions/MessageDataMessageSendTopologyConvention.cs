using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.MessageData.Configuration;

namespace ViciOne.ServiceBus.MessageData.Conventions;

/// <summary>Applies conventions for message data message send topology.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class MessageDataMessageSendTopologyConvention<TMessage> :
    IMessageDataMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    readonly IMessageDataRepository _repository;
    readonly MessageDataPolicy _policy;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="policy">The policy.</param>
    public MessageDataMessageSendTopologyConvention(IMessageDataRepository repository, MessageDataPolicy policy)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    bool IMessageSendTopologyConvention.TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
    {
        convention = this as IMessageSendTopologyConvention<T>;

        return convention != null;
    }

    bool IMessageSendTopologyConvention<TMessage>.TryGetMessageSendTopology(
        [NotNullWhen(true)] out IMessageSendTopology<TMessage>? messageSendTopology)
    {
        var specification = new PutMessageDataTransformSpecification<TMessage>(_repository, _policy);
        if (specification.TryGetSendTopology(out messageSendTopology))
            return true;

        messageSendTopology = null;
        return false;
    }
}
