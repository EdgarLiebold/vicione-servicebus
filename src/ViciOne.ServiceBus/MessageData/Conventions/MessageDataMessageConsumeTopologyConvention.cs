using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.MessageData.Configuration;

namespace ViciOne.ServiceBus.MessageData.Conventions;

/// <summary>
/// Provides a message data message consume topology convention implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MessageDataMessageConsumeTopologyConvention<TMessage> :
    IMessageDataMessageConsumeTopologyConvention<TMessage>
    where TMessage : class
{
    readonly IMessageDataRepository _repository;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    public MessageDataMessageConsumeTopologyConvention(IMessageDataRepository repository)
    {
        _repository = repository;
    }

    bool IMessageConsumeTopologyConvention.TryGetMessageConsumeTopologyConvention<T>(
        [NotNullWhen(true)] out IMessageConsumeTopologyConvention<T>? convention)
    {
        convention = this as IMessageConsumeTopologyConvention<T>;

        return convention != null;
    }

    /// <summary>
    /// Attempts to get message consume topology.
    /// </summary>
    /// <param name="messageConsumeTopology">The message consume topology value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessageConsumeTopology([NotNullWhen(true)] out IMessageConsumeTopology<TMessage>? messageConsumeTopology)
    {
        var specification = new GetMessageDataTransformSpecification<TMessage>(_repository);
        if (specification.TryGetConsumeTopology(out messageConsumeTopology))
            return true;

        messageConsumeTopology = null;
        return false;
    }
}
