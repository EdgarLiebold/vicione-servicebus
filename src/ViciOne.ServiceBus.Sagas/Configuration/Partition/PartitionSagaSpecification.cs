using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for partition saga.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class PartitionSagaSpecification<TSaga> :
    IPipeSpecification<SagaConsumeContext<TSaga>>
    where TSaga : class, ISaga
{
    readonly PartitionKeyProvider<SagaConsumeContext<TSaga>> _keyProvider;
    readonly IPartitioner _partitioner;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="partitioner">The partitioner.</param>
    /// <param name="keyProvider">The key provider.</param>
    public PartitionSagaSpecification(IPartitioner partitioner, PartitionKeyProvider<SagaConsumeContext<TSaga>> keyProvider)
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
    public void Apply(IPipeBuilder<SagaConsumeContext<TSaga>> builder)
    {
        builder.AddFilter(new PartitionFilter<SagaConsumeContext<TSaga>>(_keyProvider, _partitioner));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_keyProvider == null)
            yield return this.Failure("KeyProvider", "must not be null");
    }
}
