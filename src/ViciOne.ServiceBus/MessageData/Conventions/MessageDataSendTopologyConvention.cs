using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.MessageData.Conventions;

/// <summary>Creates per-message send conventions bound to one repository and policy owner.</summary>
internal sealed class MessageDataSendTopologyConvention :
    ISendTopologyConvention
{
    readonly ITopologyConventionCache<IMessageSendTopologyConvention> _cache;

    /// <summary>Creates a convention for one repository and policy owner.</summary>
    /// <param name="repository">The repository used for external storage.</param>
    /// <param name="policy">The inline and retention policy.</param>
    public MessageDataSendTopologyConvention(IMessageDataRepository repository, MessageDataPolicy policy)
    {
        _cache = new TopologyConventionCache<IMessageSendTopologyConvention>(new Factory(repository, policy));
    }

    /// <summary>Gets the cached message-data convention for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="convention">Receives the contract-specific convention.</param>
    /// <returns><see langword="true" /> when the contract-specific convention is available.</returns>
    public bool TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
        where T : class
    {
        return _cache.GetOrAdd<T, IMessageSendTopologyConvention<T>>().TryGetMessageSendTopologyConvention(out convention);
    }


    sealed class Factory :
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
