using System;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Quartz;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a quartz endpoint definition implementation.
/// </summary>
public class QuartzEndpointDefinition :
    IEndpointDefinition<ScheduleMessageConsumer>,
    IEndpointDefinition<CancelScheduledMessageConsumer>,
    IEndpointDefinition<PauseScheduledMessageConsumer>,
    IEndpointDefinition<ResumeScheduledMessageConsumer>
{
    readonly int? _concurrentMessageLimit;
    readonly int? _prefetchCount;
    readonly string _queueName;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    public QuartzEndpointDefinition(IOptions<QuartzEndpointOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        QuartzEndpointOptions value = options.Value;
        ArgumentException.ThrowIfNullOrWhiteSpace(value.QueueName);
        if (value.PrefetchCount is <= 0)
            throw new ArgumentOutOfRangeException(nameof(value.PrefetchCount), value.PrefetchCount, "PrefetchCount must be greater than zero.");
        if (value.ConcurrentMessageLimit is <= 0)
            throw new ArgumentOutOfRangeException(nameof(value.ConcurrentMessageLimit), value.ConcurrentMessageLimit,
                "ConcurrentMessageLimit must be greater than zero.");

        _prefetchCount = value.PrefetchCount;
        _concurrentMessageLimit = value.ConcurrentMessageLimit;
        _queueName = value.QueueName;

        Partition = new Partitioner(_concurrentMessageLimit ?? _prefetchCount ?? 32, new Murmur3UnsafeHashGenerator());
    }

    /// <summary>
    /// Gets the partition value.
    /// </summary>
    public IPartitioner Partition { get; }

    /// <summary>
    /// Gets the configure consume topology value.
    /// </summary>
    public virtual bool ConfigureConsumeTopology => true;

    /// <summary>
    /// Gets the is temporary value.
    /// </summary>
    public virtual bool IsTemporary => false;

    /// <summary>
    /// Gets the prefetch count value.
    /// </summary>
    public virtual int? PrefetchCount => _prefetchCount;

    /// <summary>
    /// Gets the concurrent message limit value.
    /// </summary>
    public virtual int? ConcurrentMessageLimit => _concurrentMessageLimit;

    string IEndpointDefinition.GetEndpointName(IEndpointNameFormatter formatter)
    {
        return _queueName;
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="context">The operation context.</param>
    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
    }
}
