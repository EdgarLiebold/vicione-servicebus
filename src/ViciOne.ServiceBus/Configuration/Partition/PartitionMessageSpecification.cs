using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for partition message.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class PartitionMessageSpecification<T> :
    IPipeSpecification<ConsumeContext<T>>
    where T : class
{
    readonly IPartitioner _partitioner;
    PartitionKeyProvider<ConsumeContext<T>> _keyProvider = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="partitioner">The partitioner.</param>
    public PartitionMessageSpecification(IPartitioner partitioner)
    {
        _partitioner = partitioner;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumeContext<T>> builder)
    {
        if (_keyProvider == null)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Partition Message", "unknown", $"The partition key provider was not found for message type: {TypeCache<T>.ShortName}", "Correct the named configuration before starting the host"));

        builder.AddFilter(new PartitionFilter<ConsumeContext<T>>(_keyProvider, _partitioner));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
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
