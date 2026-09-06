using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.MessageData.Conventions;

/// <summary>Applies conventions for message data send topology.</summary>
public class MessageDataSendTopologyConvention :
    ISendTopologyConvention
{
    readonly ITopologyConventionCache<IMessageSendTopologyConvention> _cache;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="policy">The policy.</param>
    public MessageDataSendTopologyConvention(IMessageDataRepository repository, MessageDataPolicy policy)
    {
        _cache = new TopologyConventionCache<IMessageSendTopologyConvention>(typeof(MessageDataMessageSendTopologyConvention<>),
            new Factory(repository, policy));
    }

    /// <summary>Attempts to get message send topology convention.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="convention">Receives the convention produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
        where T : class
    {
        return _cache.GetOrAdd<T, IMessageSendTopologyConvention<T>>().TryGetMessageSendTopologyConvention(out convention);
    }


    class Factory :
        IConventionTypeFactory<IMessageSendTopologyConvention>
    {
        readonly IMessageDataRepository _repository;
        readonly MessageDataPolicy _policy;

        public Factory(IMessageDataRepository repository, MessageDataPolicy policy)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        IMessageSendTopologyConvention IConventionTypeFactory<IMessageSendTopologyConvention>.Create<T>()
        {
            return new MessageDataMessageSendTopologyConvention<T>(_repository, _policy);
        }
    }
}
