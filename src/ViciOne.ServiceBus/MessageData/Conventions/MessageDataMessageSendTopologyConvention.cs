using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.MessageData.Configuration;

namespace ViciOne.ServiceBus.MessageData.Conventions;

/// <summary>
/// Provides a message data message send topology convention implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MessageDataMessageSendTopologyConvention<TMessage> :
    IMessageDataMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    readonly IMessageDataRepository _repository;
    readonly MessageDataPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="policy">The policy value.</param>
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
