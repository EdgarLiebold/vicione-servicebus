using System;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.JobService;
using JobServiceState = ViciOne.ServiceBus.JobService.JobService;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an instance job service settings implementation.
/// </summary>
public class InstanceJobServiceSettings :
    JobServiceSettings
{
    readonly JobConsumerOptions _options;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    public InstanceJobServiceSettings(IOptions<JobConsumerOptions> options)
        : this(options.Value)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    public InstanceJobServiceSettings(JobConsumerOptions options)
    {
        _options = options;

        JobService = new JobServiceState(this);
    }

    /// <summary>
    /// Gets the heartbeat interval value.
    /// </summary>
    public TimeSpan HeartbeatInterval => _options.HeartbeatInterval;
    /// <summary>
    /// Gets the rejected job delay value.
    /// </summary>
    public TimeSpan RejectedJobDelay => _options.RejectedJobDelay;
    /// <summary>
    /// Gets the time provider value.
    /// </summary>
    public TimeProvider TimeProvider => _options.TimeProvider;

    /// <summary>
    /// Gets or sets the instance address value.
    /// </summary>
    public Uri? InstanceAddress { get; set; }
    /// <summary>
    /// Gets or sets the instance endpoint configurator value.
    /// </summary>
    public IReceiveEndpointConfigurator? InstanceEndpointConfigurator { get; set; }
    /// <summary>
    /// Gets the job service value.
    /// </summary>
    public IJobService JobService { get; }

    /// <summary>
    /// Performs the apply configuration operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void ApplyConfiguration<T>(T configurator)
        where T : IReceiveEndpointConfigurator
    {
        InstanceEndpointConfigurator = configurator;

        InstanceAddress = configurator.InputAddress;
    }
}
