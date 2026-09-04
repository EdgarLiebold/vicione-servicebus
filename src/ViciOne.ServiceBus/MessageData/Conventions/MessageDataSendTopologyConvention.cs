using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.MessageData.Conventions;

public class MessageDataSendTopologyConvention :
    ISendTopologyConvention
{
    readonly ITopologyConventionCache<IMessageSendTopologyConvention> _cache;

    public MessageDataSendTopologyConvention(IMessageDataRepository repository, MessageDataPolicy policy)
    {
        _cache = new TopologyConventionCache<IMessageSendTopologyConvention>(typeof(MessageDataMessageSendTopologyConvention<>),
            new Factory(repository, policy));
    }

    public bool TryGetMessageSendTopologyConvention<T>(out IMessageSendTopologyConvention<T> convention)
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
