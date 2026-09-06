using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for partitioner pipe.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class PartitionerPipeSpecification<T> :
    IPipeSpecification<T>
    where T : class, PipeContext
{
    readonly PartitionKeyProvider<T> _keyProvider;
    readonly int _partitionCount;
    readonly IPartitioner _partitioner = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="keyProvider">The key provider.</param>
    /// <param name="partitionCount">The partition count.</param>
    public PartitionerPipeSpecification(PartitionKeyProvider<T> keyProvider, int partitionCount)
    {
        _keyProvider = keyProvider;
        _partitionCount = partitionCount;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="keyProvider">The key provider.</param>
    /// <param name="partitioner">The partitioner.</param>
    public PartitionerPipeSpecification(PartitionKeyProvider<T> keyProvider, IPartitioner partitioner)
    {
        _keyProvider = keyProvider;
        _partitioner = partitioner;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<T> builder)
    {
        var partitioner = _partitioner ?? new Partitioner(_partitionCount, new Murmur3UnsafeHashGenerator());

        builder.AddFilter(new PartitionFilter<T>(_keyProvider, partitioner));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_keyProvider == null)
            yield return this.Failure("KeyProvider", "must not be null");
        if (_partitioner == null && _partitionCount < 1)
            yield return this.Failure("PartitionCount", "must be >= 1");
    }
}
