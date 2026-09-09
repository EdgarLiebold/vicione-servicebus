using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware.Partitioning;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Derives message partition keys from the registered correlation convention.</summary>
/// <typeparam name="T">The consumed message contract.</typeparam>
internal sealed class PartitionMessageSpecification<T> :
    IPipeSpecification<ConsumeContext<T>>
    where T : class
{
    readonly IPartitioner _partitioner;

    /// <summary>Captures the partitioner shared across configured message types.</summary>
    /// <param name="partitioner">The partitioner shared across the observed message types.</param>
    public PartitionMessageSpecification(IPartitioner partitioner)
    {
        _partitioner = partitioner ?? throw new ArgumentNullException(nameof(partitioner));
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumeContext<T>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (!TryCreateKeyProvider(out PartitionKeyProvider<ConsumeContext<T>>? keyProvider))
        {
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                "Partition Message",
                "unknown",
                $"The partition key provider was not found for message type: {TypeCache<T>.ShortName}",
                "Correct the named configuration before starting the host"));
        }

        builder.AddFilter(new PartitionFilter<ConsumeContext<T>>(keyProvider, _partitioner));
    }

    /// <summary>Resolves and captures the message type's correlation convention.</summary>
    /// <returns>A failure when the message type has no correlation convention; otherwise an empty sequence.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (!TryCreateKeyProvider(out _))
            yield return this.Failure("Partition", TypeCache<T>.ShortName, "A CorrelationId convention for this message type was not found.");
    }

    static bool TryCreateKeyProvider([NotNullWhen(true)] out PartitionKeyProvider<ConsumeContext<T>>? keyProvider)
    {
        if (GlobalTopology.Send.GetMessageTopology<T>().TryGetConvention(out ICorrelationIdMessageSendTopologyConvention<T>? convention)
            && convention.TryGetMessageCorrelationId(out IMessageCorrelationId<T>? messageCorrelationId))
        {
            keyProvider = context => messageCorrelationId.TryGetCorrelationId(context.Message, out Guid correlationId)
                ? correlationId.ToByteArray()
                : throw new InvalidOperationException(
                    $"The message of type {TypeCache<T>.ShortName} does not contain a correlation identifier.");
            return true;
        }

        keyProvider = null;
        return false;
    }
}
