using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a partition saga specification implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class PartitionSagaSpecification<TSaga> :
    IPipeSpecification<SagaConsumeContext<TSaga>>
    where TSaga : class, ISaga
{
    readonly PartitionKeyProvider<SagaConsumeContext<TSaga>> _keyProvider;
    readonly IPartitioner _partitioner;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="partitioner">The partitioner value.</param>
    /// <param name="keyProvider">The key provider value.</param>
    public PartitionSagaSpecification(IPartitioner partitioner, PartitionKeyProvider<SagaConsumeContext<TSaga>> keyProvider)
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
    public void Apply(IPipeBuilder<SagaConsumeContext<TSaga>> builder)
    {
        builder.AddFilter(new PartitionFilter<SagaConsumeContext<TSaga>>(_keyProvider, _partitioner));
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
