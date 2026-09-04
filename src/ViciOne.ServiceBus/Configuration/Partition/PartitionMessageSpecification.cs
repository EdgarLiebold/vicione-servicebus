using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a partition message specification implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class PartitionMessageSpecification<T> :
    IPipeSpecification<ConsumeContext<T>>
    where T : class
{
    readonly IPartitioner _partitioner;
    PartitionKeyProvider<ConsumeContext<T>> _keyProvider = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="partitioner">The partitioner value.</param>
    public PartitionMessageSpecification(IPartitioner partitioner)
    {
        _partitioner = partitioner;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<ConsumeContext<T>> builder)
    {
        if (_keyProvider == null)
            throw new ConfigurationException($"The partition key provider was not found for message type: {TypeCache<T>.ShortName}");

        builder.AddFilter(new PartitionFilter<ConsumeContext<T>>(_keyProvider, _partitioner));
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (GlobalTopology.Send.GetMessageTopology<T>().TryGetConvention(out ICorrelationIdMessageSendTopologyConvention<T>? convention)
            && convention.TryGetMessageCorrelationId(out IMessageCorrelationId<T>? messageCorrelationId))
        {
            _keyProvider = context => messageCorrelationId.TryGetCorrelationId(context.Message, out var correlationId)
                ? correlationId.ToByteArray()
                : default(Guid).ToByteArray();
        }
        else
            yield return this.Failure("Partition", TypeCache<T>.ShortName, "A CorrelationId convention for this message type was not found.");
    }
}
