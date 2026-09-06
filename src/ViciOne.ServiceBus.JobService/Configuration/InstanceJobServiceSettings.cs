using System;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.JobService;
using JobServiceState = ViciOne.ServiceBus.JobService.JobService;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Owns the job runtime and endpoint identity for one configured service instance.</summary>
internal sealed class InstanceJobServiceSettings :
    JobServiceSettings
{
    readonly JobConsumerOptions _options;

    /// <summary>Creates settings from the registered job-consumer options.</summary>
    /// <param name="options">The options monitor resolved for the service instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options" /> is <see langword="null" />.</exception>
    public InstanceJobServiceSettings(IOptions<JobConsumerOptions> options)
        : this(GetValue(options))
    {
    }

    /// <summary>Creates local runtime settings from direct job-service configuration.</summary>
    /// <param name="options">The options shared by the direct endpoints and local runtime.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options" /> is <see langword="null" />.</exception>
    public InstanceJobServiceSettings(JobServiceOptions options)
        : this(CreateConsumerOptions(options))
    {
    }

    /// <summary>Creates settings from explicit job-consumer options.</summary>
    /// <param name="options">The options used by the local runtime.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options" /> is <see langword="null" />.</exception>
    public InstanceJobServiceSettings(JobConsumerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        Runtime = new JobServiceState(this);
    }

    /// <inheritdoc />
    public TimeSpan HeartbeatInterval => _options.HeartbeatInterval;

    /// <inheritdoc />
    public TimeSpan RejectedJobDelay => _options.RejectedJobDelay;

    /// <inheritdoc />
    public TimeProvider TimeProvider => _options.TimeProvider;

    /// <inheritdoc />
    public Uri? InstanceAddress { get; private set; }

    /// <inheritdoc />
    public IReceiveEndpointConfigurator? InstanceEndpoint { get; private set; }

    /// <inheritdoc />
    public IJobService Runtime { get; }

    /// <summary>Captures the endpoint used to execute jobs for this service instance.</summary>
    /// <typeparam name="TConfigurator">The concrete endpoint-configurator type.</typeparam>
    /// <param name="configurator">The configured service-instance endpoint.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configurator" /> is <see langword="null" />.</exception>
    public void ApplyConfiguration<TConfigurator>(TConfigurator configurator)
        where TConfigurator : IReceiveEndpointConfigurator
    {
        ArgumentNullException.ThrowIfNull(configurator);

        InstanceEndpoint = configurator;
        InstanceAddress = configurator.InputAddress;
    }

    static JobConsumerOptions GetValue(IOptions<JobConsumerOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.Value ?? throw new ArgumentException("The options wrapper returned no value.", nameof(options));
    }

    static JobConsumerOptions CreateConsumerOptions(JobServiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new JobConsumerOptions
        {
            HeartbeatInterval = options.HeartbeatInterval,
            RejectedJobDelay = options.RejectedJobDelay,
            TimeProvider = options.TimeProvider,
        };
    }
}
