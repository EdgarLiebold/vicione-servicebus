using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.MessageData.Configuration;

namespace ViciOne.ServiceBus.MessageData.Conventions;

/// <summary>Creates a consume transform only when a message contract contains message-data properties.</summary>
/// <typeparam name="TMessage">The message contract inspected by the convention.</typeparam>
internal sealed class MessageDataMessageConsumeTopologyConvention<TMessage> :
    IMessageDataMessageConsumeTopologyConvention<TMessage>
    where TMessage : class
{
    readonly IMessageDataRepository _repository;

    /// <summary>Creates a contract-specific convention for one repository.</summary>
    /// <param name="repository">The repository that owns external references.</param>
    public MessageDataMessageConsumeTopologyConvention(IMessageDataRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    bool IMessageConsumeTopologyConvention.TryGetMessageConsumeTopologyConvention<T>(
        [NotNullWhen(true)] out IMessageConsumeTopologyConvention<T>? convention)
    {
        convention = this as IMessageConsumeTopologyConvention<T>;

        return convention != null;
    }

    /// <summary>Creates the consume transformation required by this contract.</summary>
    /// <param name="messageConsumeTopology">Receives the topology when message-data properties were discovered.</param>
    /// <returns><see langword="true" /> when the contract requires a message-data transformation.</returns>
    public bool TryGetMessageConsumeTopology([NotNullWhen(true)] out IMessageConsumeTopology<TMessage>? messageConsumeTopology)
    {
        var specification = new GetMessageDataTransformSpecification<TMessage>(_repository);
        if (specification.TryGetConsumeTopology(out messageConsumeTopology))
            return true;

        messageConsumeTopology = null;
        return false;
    }
}
