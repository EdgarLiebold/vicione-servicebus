using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.MessageData.Configuration;

namespace ViciOne.ServiceBus.MessageData.Conventions;

/// <summary>Creates a send transform only when a message contract contains message-data properties.</summary>
/// <typeparam name="TMessage">The message contract inspected by the convention.</typeparam>
internal sealed class MessageDataMessageSendTopologyConvention<TMessage> :
    IMessageDataMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    readonly IMessageDataRepository _repository;
    readonly MessageDataPolicy _policy;

    /// <summary>Creates a contract-specific convention for one repository and policy owner.</summary>
    /// <param name="repository">The repository used for external storage.</param>
    /// <param name="policy">The inline and retention policy.</param>
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
