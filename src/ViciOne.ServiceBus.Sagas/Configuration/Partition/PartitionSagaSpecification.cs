using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware.Partitioning;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Serializes saga-consume contexts that resolve to the same partition key.</summary>
/// <typeparam name="TSaga">The saga state carried by the consume context.</typeparam>
internal sealed class PartitionSagaSpecification<TSaga> :
    IPipeSpecification<SagaConsumeContext<TSaga>>
    where TSaga : class, ISaga
{
    readonly PartitionKeyProvider<SagaConsumeContext<TSaga>> _keyProvider;
    readonly IPartitioner _partitioner;

    /// <summary>Creates a saga specification backed by a shared partitioner and correlation-key selector.</summary>
    /// <param name="partitioner">The shared partition owner.</param>
    /// <param name="keyProvider">The function that extracts stable partition bytes from each saga context.</param>
    internal PartitionSagaSpecification(IPartitioner partitioner, PartitionKeyProvider<SagaConsumeContext<TSaga>> keyProvider)
    {
        ArgumentNullException.ThrowIfNull(partitioner);
        ArgumentNullException.ThrowIfNull(keyProvider);

        _partitioner = partitioner;
        _keyProvider = keyProvider;
    }

    /// <summary>Adds the configured saga partition filter to a consume-pipeline builder.</summary>
    /// <param name="builder">The saga consume-pipeline builder.</param>
    public void Apply(IPipeBuilder<SagaConsumeContext<TSaga>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddFilter(new PartitionFilter<SagaConsumeContext<TSaga>>(_keyProvider, _partitioner));
    }

    /// <summary>Returns no deferred failures because construction validates every required value.</summary>
    /// <returns>An empty validation result sequence.</returns>
    public IEnumerable<ValidationResult> Validate() => Array.Empty<ValidationResult>();
}
