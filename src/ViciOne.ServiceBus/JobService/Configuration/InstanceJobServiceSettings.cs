using System;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.JobService;
using JobServiceState = ViciOne.ServiceBus.JobService.JobService;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines settings for instance job service.</summary>
public class InstanceJobServiceSettings :
    JobServiceSettings
{
    readonly JobConsumerOptions _options;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="options">The options that control the operation.</param>
    public InstanceJobServiceSettings(IOptions<JobConsumerOptions> options)
        : this(options.Value)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="options">The options that control the operation.</param>
    public InstanceJobServiceSettings(JobConsumerOptions options)
    {
        _options = options;

        JobService = new JobServiceState(this);
    }

    /// <summary>Gets the heartbeat interval.</summary>
    public TimeSpan HeartbeatInterval => _options.HeartbeatInterval;
    /// <summary>Gets the rejected job delay.</summary>
    public TimeSpan RejectedJobDelay => _options.RejectedJobDelay;
    /// <summary>Gets the time provider.</summary>
    public TimeProvider TimeProvider => _options.TimeProvider;

    /// <summary>Gets or sets the instance address.</summary>
    public Uri? InstanceAddress { get; set; }
    /// <summary>Gets or sets the instance endpoint configurator.</summary>
    public IReceiveEndpointConfigurator? InstanceEndpointConfigurator { get; set; }
    /// <summary>Gets the job service.</summary>
    public IJobService JobService { get; }

    /// <summary>Applies configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void ApplyConfiguration<T>(T configurator)
        where T : IReceiveEndpointConfigurator
    {
        InstanceEndpointConfigurator = configurator;

        InstanceAddress = configurator.InputAddress;
    }
}
