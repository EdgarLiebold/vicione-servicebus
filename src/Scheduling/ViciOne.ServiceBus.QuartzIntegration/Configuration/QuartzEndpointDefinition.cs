namespace ViciOne.ServiceBus.Configuration
{
    using System;
    using Microsoft.Extensions.Options;
    using Middleware;
    using QuartzIntegration;


    public class QuartzEndpointDefinition :
        IEndpointDefinition<ScheduleMessageConsumer>,
        IEndpointDefinition<CancelScheduledMessageConsumer>,
        IEndpointDefinition<PauseScheduledMessageConsumer>,
        IEndpointDefinition<ResumeScheduledMessageConsumer>
    {
        readonly int? _concurrentMessageLimit;
        readonly int? _prefetchCount;
        readonly string _queueName;

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

        public IPartitioner Partition { get; }

        public virtual bool ConfigureConsumeTopology => true;

        public virtual bool IsTemporary => false;

        public virtual int? PrefetchCount => _prefetchCount;

        public virtual int? ConcurrentMessageLimit => _concurrentMessageLimit;

        string IEndpointDefinition.GetEndpointName(IEndpointNameFormatter formatter)
        {
            return _queueName;
        }

        public void Configure<T>(T configurator, IRegistrationContext? context)
            where T : IReceiveEndpointConfigurator
        {
        }
    }
}
