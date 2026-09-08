using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Quartz.Consumers;

namespace ViciOne.ServiceBus.Quartz.Configuration;

/// <summary>Defines the shared receive endpoint and partitioner for Quartz scheduling commands.</summary>
internal sealed class QuartzEndpointDefinition<TBus> :
    IEndpointDefinition<ScheduleMessageConsumer<TBus>>,
    IEndpointDefinition<CancelScheduledMessageConsumer<TBus>>,
    IEndpointDefinition<PauseScheduledMessageConsumer<TBus>>,
    IEndpointDefinition<ResumeScheduledMessageConsumer<TBus>>,
    IAsyncDisposable
    where TBus : class, IBus
{
    readonly int? _concurrentMessageLimit;
    readonly int? _prefetchCount;
    readonly string _queueName;

    /// <summary>Initializes the endpoint definition from validated immutable settings.</summary>
    /// <param name="settings">The bus-specific endpoint settings.</param>
    public QuartzEndpointDefinition(QuartzEndpointSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _prefetchCount = settings.PrefetchCount;
        _concurrentMessageLimit = settings.ConcurrentMessageLimit;
        _queueName = settings.QueueName;

        Partition = new Partitioner(_concurrentMessageLimit ?? _prefetchCount ?? 32, new Murmur3UnsafeHashGenerator());
    }

    /// <summary>Gets the partitioner that serializes commands for the same trigger identity.</summary>
    public IPartitioner Partition { get; }

    /// <summary>Gets whether the endpoint configures consume topology for scheduling contracts.</summary>
    public bool ConfigureConsumeTopology => true;

    /// <summary>Gets whether the scheduling endpoint is temporary.</summary>
    public bool IsTemporary => false;

    /// <summary>Gets the configured transport prefetch count.</summary>
    public int? PrefetchCount => _prefetchCount;

    /// <summary>Gets the configured maximum number of concurrent scheduling commands.</summary>
    public int? ConcurrentMessageLimit => _concurrentMessageLimit;

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

    /// <summary>Releases the partitioner owned by the endpoint definition.</summary>
    /// <returns>An awaitable disposal operation.</returns>
    public ValueTask DisposeAsync()
    {
        return Partition.DisposeAsync();
    }
}
