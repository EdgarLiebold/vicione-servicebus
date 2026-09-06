using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.JobService;

/// <summary>
/// Settings relevant to the job consumer endpoints and the service instance
/// </summary>
public interface JobServiceSettings :
    IOptions
{
    /// <summary>
    /// Gets the job service value.
    /// </summary>
    IJobService JobService { get; }

    /// <summary>
    /// Gets the heartbeat interval value.
    /// </summary>
    TimeSpan HeartbeatInterval { get; }

    /// <summary>
    /// Adjust the time delay before a rejected job is retried
    /// </summary>
    TimeSpan RejectedJobDelay { get; }

    /// <summary>
    /// Gets the time provider value.
    /// </summary>
    TimeProvider TimeProvider { get; }

    /// <summary>
    /// Gets the instance address value.
    /// </summary>
    Uri? InstanceAddress { get; }

    /// <summary>
    /// Gets the instance endpoint configurator value.
    /// </summary>
    IReceiveEndpointConfigurator? InstanceEndpointConfigurator { get; }
}
