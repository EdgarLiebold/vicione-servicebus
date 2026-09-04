using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a partitioner pipe specification implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class PartitionerPipeSpecification<T> :
    IPipeSpecification<T>
    where T : class, PipeContext
{
    readonly PartitionKeyProvider<T> _keyProvider;
    readonly int _partitionCount;
    readonly IPartitioner _partitioner = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="keyProvider">The key provider value.</param>
    /// <param name="partitionCount">The partition count value.</param>
    public PartitionerPipeSpecification(PartitionKeyProvider<T> keyProvider, int partitionCount)
    {
        _keyProvider = keyProvider;
        _partitionCount = partitionCount;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="keyProvider">The key provider value.</param>
    /// <param name="partitioner">The partitioner value.</param>
    public PartitionerPipeSpecification(PartitionKeyProvider<T> keyProvider, IPartitioner partitioner)
    {
        _keyProvider = keyProvider;
        _partitioner = partitioner;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<T> builder)
    {
        var partitioner = _partitioner ?? new Partitioner(_partitionCount, new Murmur3UnsafeHashGenerator());

        builder.AddFilter(new PartitionFilter<T>(_keyProvider, partitioner));
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_keyProvider == null)
            yield return this.Failure("KeyProvider", "must not be null");
        if (_partitioner == null && _partitionCount < 1)
            yield return this.Failure("PartitionCount", "must be >= 1");
    }
}
