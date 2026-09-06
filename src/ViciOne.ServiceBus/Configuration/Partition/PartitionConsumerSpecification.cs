using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for partition consumer.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public class PartitionConsumerSpecification<TConsumer> :
    IPipeSpecification<ConsumerConsumeContext<TConsumer>>
    where TConsumer : class
{
    readonly PartitionKeyProvider<ConsumerConsumeContext<TConsumer>> _keyProvider;
    readonly IPartitioner _partitioner;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="partitioner">The partitioner.</param>
    /// <param name="keyProvider">The key provider.</param>
    public PartitionConsumerSpecification(IPartitioner partitioner, PartitionKeyProvider<ConsumerConsumeContext<TConsumer>> keyProvider)
    {
        if (partitioner == null)
            throw new ArgumentNullException(nameof(partitioner));
        if (keyProvider == null)
            throw new ArgumentNullException(nameof(keyProvider));

        _partitioner = partitioner;
        _keyProvider = keyProvider;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumerConsumeContext<TConsumer>> builder)
    {
        builder.AddFilter(new PartitionFilter<ConsumerConsumeContext<TConsumer>>(_keyProvider, _partitioner));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_keyProvider == null)
            yield return this.Failure("KeyProvider", "must not be null");
    }
}
