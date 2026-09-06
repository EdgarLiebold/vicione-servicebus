using System;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Quartz;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the shared receive endpoint and partitioner for Quartz scheduling commands.</summary>
public class QuartzEndpointDefinition :
    IEndpointDefinition<ScheduleMessageConsumer>,
    IEndpointDefinition<CancelScheduledMessageConsumer>,
    IEndpointDefinition<PauseScheduledMessageConsumer>,
    IEndpointDefinition<ResumeScheduledMessageConsumer>
{
    readonly int? _concurrentMessageLimit;
    readonly int? _prefetchCount;
    readonly string _queueName;

    /// <summary>Initializes the endpoint definition from validated Quartz endpoint options.</summary>
    /// <param name="options">The validated queue, prefetch, and concurrency settings.</param>
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

    /// <summary>Gets the partitioner that serializes commands for the same trigger identity.</summary>
    public IPartitioner Partition { get; }

    /// <summary>Gets whether the endpoint configures consume topology for scheduling contracts.</summary>
    public virtual bool ConfigureConsumeTopology => true;

    /// <summary>Gets whether the scheduling endpoint is temporary.</summary>
    public virtual bool IsTemporary => false;

    /// <summary>Gets the configured transport prefetch count.</summary>
    public virtual int? PrefetchCount => _prefetchCount;

    /// <summary>Gets the configured maximum number of concurrent scheduling commands.</summary>
    public virtual int? ConcurrentMessageLimit => _concurrentMessageLimit;

    string IEndpointDefinition.GetEndpointName(IEndpointNameFormatter formatter)
    {
        return _queueName;
    }

    /// <summary>Leaves provider-specific endpoint settings unchanged.</summary>
    /// <typeparam name="T">The concrete receive-endpoint configurator type.</typeparam>
    /// <param name="configurator">The receive endpoint being configured.</param>
    /// <param name="context">The optional registration context.</param>
    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
    }
}
