using System;
using System.Buffers.Binary;
using System.Text;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Partitioning;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures deterministic, keyed serialization on message pipelines.</summary>
public static class PartitionerConfigurationExtensions
{
    /// <summary>
    /// Partitions every non-batch message type on a consume pipeline by its registered correlation identifier.
    /// </summary>
    /// <param name="configurator">The consume pipeline to partition.</param>
    /// <param name="partitionCount">The positive number of independently serialized partitions.</param>
    public static void UseMessagePartitioner(this IConsumePipeConfigurator configurator, int partitionCount)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentOutOfRangeException.ThrowIfLessThan(partitionCount, 1);

        var partitioner = new PartitionCoordinator(partitionCount);
        _ = new PartitionMessageConfigurationObserver(configurator, partitioner);
    }

    /// <summary>Partitions one message type on a consume pipeline by a correlation identifier.</summary>
    /// <typeparam name="TMessage">The consumed message type.</typeparam>
    /// <param name="configurator">The consume pipeline to partition.</param>
    /// <param name="partitioner">The shared partitioner whose lifetime remains caller-owned.</param>
    /// <param name="keyProvider">The function that selects a stable key from each consume context.</param>
    public static void UsePartitioner<TMessage>(this IConsumePipeConfigurator configurator, IPartitioner partitioner,
        Func<ConsumeContext<TMessage>, Guid> keyProvider)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(partitioner);
        ArgumentNullException.ThrowIfNull(keyProvider);

        byte[] GetPartitionKey(ConsumeContext<TMessage> context) => keyProvider(context).ToByteArray();

        configurator.AddPipeSpecification(
            new PartitionerPipeSpecification<ConsumeContext<TMessage>>(GetPartitionKey, partitioner));
    }

    /// <summary>Partitions pipeline operations by a binary key using a dedicated partitioner.</summary>
    /// <typeparam name="TContext">The context type carried by the pipeline.</typeparam>
    /// <param name="configurator">The pipeline to partition.</param>
    /// <param name="partitionCount">The positive number of independently serialized partitions.</param>
    /// <param name="keyProvider">The function that returns a stable, non-null binary key.</param>
    public static void UsePartitioner<TContext>(this IPipeConfigurator<TContext> configurator, int partitionCount,
        Func<TContext, byte[]> keyProvider)
        where TContext : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentOutOfRangeException.ThrowIfLessThan(partitionCount, 1);
        ArgumentNullException.ThrowIfNull(keyProvider);

        byte[] GetPartitionKey(TContext context) => RequireKey(keyProvider(context));

        configurator.AddPipeSpecification(new PartitionerPipeSpecification<TContext>(GetPartitionKey, partitionCount));
    }

    /// <summary>Partitions pipeline operations by a binary key using a shared partitioner.</summary>
    /// <typeparam name="TContext">The context type carried by the pipeline.</typeparam>
    /// <param name="configurator">The pipeline to partition.</param>
    /// <param name="partitioner">The shared partitioner whose lifetime remains caller-owned.</param>
    /// <param name="keyProvider">The function that returns a stable, non-null binary key.</param>
    public static void UsePartitioner<TContext>(this IPipeConfigurator<TContext> configurator, IPartitioner partitioner,
        Func<TContext, byte[]> keyProvider)
        where TContext : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(partitioner);
        ArgumentNullException.ThrowIfNull(keyProvider);

        byte[] GetPartitionKey(TContext context) => RequireKey(keyProvider(context));

        configurator.AddPipeSpecification(new PartitionerPipeSpecification<TContext>(GetPartitionKey, partitioner));
    }

    /// <summary>Partitions pipeline operations by a <see cref="Guid" /> key using a dedicated partitioner.</summary>
    /// <typeparam name="TContext">The context type carried by the pipeline.</typeparam>
    /// <param name="configurator">The pipeline to partition.</param>
    /// <param name="partitionCount">The positive number of independently serialized partitions.</param>
    /// <param name="keyProvider">The function that selects a stable identifier from each context.</param>
    public static void UsePartitioner<TContext>(this IPipeConfigurator<TContext> configurator, int partitionCount,
        Func<TContext, Guid> keyProvider)
        where TContext : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentOutOfRangeException.ThrowIfLessThan(partitionCount, 1);
        ArgumentNullException.ThrowIfNull(keyProvider);

        configurator.UsePartitioner(partitionCount, context => keyProvider(context).ToByteArray());
    }

    /// <summary>Partitions pipeline operations by a <see cref="Guid" /> key using a shared partitioner.</summary>
    /// <typeparam name="TContext">The context type carried by the pipeline.</typeparam>
    /// <param name="configurator">The pipeline to partition.</param>
    /// <param name="partitioner">The shared partitioner whose lifetime remains caller-owned.</param>
    /// <param name="keyProvider">The function that selects a stable identifier from each context.</param>
    public static void UsePartitioner<TContext>(this IPipeConfigurator<TContext> configurator, IPartitioner partitioner,
        Func<TContext, Guid> keyProvider)
        where TContext : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(partitioner);
        ArgumentNullException.ThrowIfNull(keyProvider);

        configurator.UsePartitioner(partitioner, context => keyProvider(context).ToByteArray());
    }

    /// <summary>Partitions pipeline operations by a text key using a dedicated partitioner.</summary>
    /// <typeparam name="TContext">The context type carried by the pipeline.</typeparam>
    /// <param name="configurator">The pipeline to partition.</param>
    /// <param name="partitionCount">The positive number of independently serialized partitions.</param>
    /// <param name="keyProvider">The function that returns a stable, non-null text key.</param>
    /// <param name="encoding">The encoding used to produce key bytes; defaults to UTF-8.</param>
    public static void UsePartitioner<TContext>(this IPipeConfigurator<TContext> configurator, int partitionCount,
        Func<TContext, string> keyProvider, Encoding? encoding = null)
        where TContext : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentOutOfRangeException.ThrowIfLessThan(partitionCount, 1);
        ArgumentNullException.ThrowIfNull(keyProvider);

        Encoding textEncoding = encoding ?? Encoding.UTF8;
        configurator.UsePartitioner(partitionCount, context => GetBytes(keyProvider(context), textEncoding));
    }

    /// <summary>Partitions pipeline operations by a text key using a shared partitioner.</summary>
    /// <typeparam name="TContext">The context type carried by the pipeline.</typeparam>
    /// <param name="configurator">The pipeline to partition.</param>
    /// <param name="partitioner">The shared partitioner whose lifetime remains caller-owned.</param>
    /// <param name="keyProvider">The function that returns a stable, non-null text key.</param>
    /// <param name="encoding">The encoding used to produce key bytes; defaults to UTF-8.</param>
    public static void UsePartitioner<TContext>(this IPipeConfigurator<TContext> configurator, IPartitioner partitioner,
        Func<TContext, string> keyProvider, Encoding? encoding = null)
        where TContext : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(partitioner);
        ArgumentNullException.ThrowIfNull(keyProvider);

        Encoding textEncoding = encoding ?? Encoding.UTF8;
        configurator.UsePartitioner(partitioner, context => GetBytes(keyProvider(context), textEncoding));
    }

    /// <summary>Partitions pipeline operations by a signed 64-bit key using a dedicated partitioner.</summary>
    /// <typeparam name="TContext">The context type carried by the pipeline.</typeparam>
    /// <param name="configurator">The pipeline to partition.</param>
    /// <param name="partitionCount">The positive number of independently serialized partitions.</param>
    /// <param name="keyProvider">The function that selects a stable integer key from each context.</param>
    public static void UsePartitioner<TContext>(this IPipeConfigurator<TContext> configurator, int partitionCount,
        Func<TContext, long> keyProvider)
        where TContext : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentOutOfRangeException.ThrowIfLessThan(partitionCount, 1);
        ArgumentNullException.ThrowIfNull(keyProvider);

        configurator.UsePartitioner(partitionCount, context => GetBytes(keyProvider(context)));
    }

    /// <summary>Partitions pipeline operations by a signed 64-bit key using a shared partitioner.</summary>
    /// <typeparam name="TContext">The context type carried by the pipeline.</typeparam>
    /// <param name="configurator">The pipeline to partition.</param>
    /// <param name="partitioner">The shared partitioner whose lifetime remains caller-owned.</param>
    /// <param name="keyProvider">The function that selects a stable integer key from each context.</param>
    public static void UsePartitioner<TContext>(this IPipeConfigurator<TContext> configurator, IPartitioner partitioner,
        Func<TContext, long> keyProvider)
        where TContext : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(partitioner);
        ArgumentNullException.ThrowIfNull(keyProvider);

        configurator.UsePartitioner(partitioner, context => GetBytes(keyProvider(context)));
    }

    static byte[] GetBytes(string key, Encoding encoding)
    {
        if (key == null)
            throw new InvalidOperationException("The partition key provider returned null.");

        return encoding.GetBytes(key);
    }

    static byte[] GetBytes(long key)
    {
        var bytes = new byte[sizeof(long)];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, key);
        return bytes;
    }

    static byte[] RequireKey(byte[] key) =>
        key ?? throw new InvalidOperationException("The partition key provider returned null.");
}
