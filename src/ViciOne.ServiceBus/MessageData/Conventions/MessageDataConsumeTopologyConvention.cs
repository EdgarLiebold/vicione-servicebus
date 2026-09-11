using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.MessageData.Conventions;

/// <summary>Creates per-message consume conventions bound to one message-data repository.</summary>
internal sealed class MessageDataConsumeTopologyConvention :
    IConsumeTopologyConvention
{
    readonly ITopologyConventionCache<IMessageConsumeTopologyConvention> _cache;

    /// <summary>Creates a convention for one repository owner.</summary>
    /// <param name="repository">The repository that owns external references.</param>
    public MessageDataConsumeTopologyConvention(IMessageDataRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _cache = new TopologyConventionCache<IMessageConsumeTopologyConvention>(typeof(MessageDataMessageConsumeTopologyConvention<>),
            new Factory(repository));
    }

    /// <summary>Gets the cached message-data convention for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="convention">Receives the contract-specific convention.</param>
    /// <returns><see langword="true" /> when the contract-specific convention is available.</returns>
    public bool TryGetMessageConsumeTopologyConvention<T>([NotNullWhen(true)] out IMessageConsumeTopologyConvention<T>? convention)
        where T : class
    {
        return _cache.GetOrAdd<T, IMessageConsumeTopologyConvention<T>>().TryGetMessageConsumeTopologyConvention(out convention);
    }


    sealed class Factory :
        IConventionTypeFactory<IMessageConsumeTopologyConvention>
    {
        readonly IMessageDataRepository _repository;

        public Factory(IMessageDataRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        IMessageConsumeTopologyConvention IConventionTypeFactory<IMessageConsumeTopologyConvention>.Create<T>()
        {
            return new MessageDataMessageConsumeTopologyConvention<T>(_repository);
        }
    }
}
