using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a partition consumer specification implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public class PartitionConsumerSpecification<TConsumer> :
    IPipeSpecification<ConsumerConsumeContext<TConsumer>>
    where TConsumer : class
{
    readonly PartitionKeyProvider<ConsumerConsumeContext<TConsumer>> _keyProvider;
    readonly IPartitioner _partitioner;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="partitioner">The partitioner value.</param>
    /// <param name="keyProvider">The key provider value.</param>
    public PartitionConsumerSpecification(IPartitioner partitioner, PartitionKeyProvider<ConsumerConsumeContext<TConsumer>> keyProvider)
    {
        if (partitioner == null)
            throw new ArgumentNullException(nameof(partitioner));
        if (keyProvider == null)
            throw new ArgumentNullException(nameof(keyProvider));

        _partitioner = partitioner;
        _keyProvider = keyProvider;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<ConsumerConsumeContext<TConsumer>> builder)
    {
        builder.AddFilter(new PartitionFilter<ConsumerConsumeContext<TConsumer>>(_keyProvider, _partitioner));
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_keyProvider == null)
            yield return this.Failure("KeyProvider", "must not be null");
    }
}
