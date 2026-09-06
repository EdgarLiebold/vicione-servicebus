using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.MessageData.Conventions;

/// <summary>Applies conventions for message data consume topology.</summary>
public class MessageDataConsumeTopologyConvention :
    IConsumeTopologyConvention
{
    readonly ITopologyConventionCache<IMessageConsumeTopologyConvention> _cache;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="repository">The repository.</param>
    public MessageDataConsumeTopologyConvention(IMessageDataRepository repository)
    {
        _cache = new TopologyConventionCache<IMessageConsumeTopologyConvention>(typeof(MessageDataMessageConsumeTopologyConvention<>),
            new Factory(repository));
    }

    /// <summary>Attempts to get message consume topology convention.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="convention">Receives the convention produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessageConsumeTopologyConvention<T>([NotNullWhen(true)] out IMessageConsumeTopologyConvention<T>? convention)
        where T : class
    {
        return _cache.GetOrAdd<T, IMessageConsumeTopologyConvention<T>>().TryGetMessageConsumeTopologyConvention(out convention);
    }


    class Factory :
        IConventionTypeFactory<IMessageConsumeTopologyConvention>
    {
        readonly IMessageDataRepository _repository;

        public Factory(IMessageDataRepository repository)
        {
            _repository = repository;
        }

        IMessageConsumeTopologyConvention IConventionTypeFactory<IMessageConsumeTopologyConvention>.Create<T>()
        {
            return new MessageDataMessageConsumeTopologyConvention<T>(_repository);
        }
    }
}
