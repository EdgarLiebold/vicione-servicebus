using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware.Partitioning;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds keyed, serialized partition admission to a pipeline.</summary>
/// <typeparam name="T">The pipeline context type.</typeparam>
internal sealed class PartitionerPipeSpecification<T> :
    IPipeSpecification<T>
    where T : class, PipeContext
{
    readonly PartitionKeyProvider<T> _keyProvider;
    readonly int _partitionCount;
    readonly IPartitioner? _partitioner;

    /// <summary>Captures a key selector and creates a dedicated partitioner when applied.</summary>
    /// <param name="keyProvider">The function that selects a stable binary key.</param>
    /// <param name="partitionCount">The number of independently serialized partitions.</param>
    public PartitionerPipeSpecification(PartitionKeyProvider<T> keyProvider, int partitionCount)
    {
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
        _partitionCount = partitionCount;
    }

    /// <summary>Captures a key selector and a partitioner shared with other pipelines.</summary>
    /// <param name="keyProvider">The function that selects a stable binary key.</param>
    /// <param name="partitioner">The shared partitioner whose lifetime remains caller-owned.</param>
    public PartitionerPipeSpecification(PartitionKeyProvider<T> keyProvider, IPartitioner partitioner)
    {
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
        _partitioner = partitioner ?? throw new ArgumentNullException(nameof(partitioner));
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<T> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        IPartitioner partitioner = _partitioner ?? new PartitionCoordinator(_partitionCount);

        builder.AddFilter(new PartitionFilter<T>(_keyProvider, partitioner));
    }

    /// <summary>Validates the requested number of dedicated partitions.</summary>
    /// <returns>Every validation failure found in the partition policy.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_partitioner == null && _partitionCount < 1)
            yield return this.Failure("PartitionCount", "must be >= 1");
    }
}
