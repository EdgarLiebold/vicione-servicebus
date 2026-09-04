using System;
using ViciOne.ServiceBus.Configuration;

#nullable enable
namespace ViciOne.ServiceBus.JobService;

/// <summary>
/// Settings relevant to the job consumer endpoints and the service instance
/// </summary>
public interface JobServiceSettings :
    IOptions
{
    IJobService JobService { get; }

    TimeSpan HeartbeatInterval { get; }

    /// <summary>
    /// Adjust the time delay before a rejected job is retried
    /// </summary>
    TimeSpan RejectedJobDelay { get; }

    TimeProvider TimeProvider { get; }

    Uri? InstanceAddress { get; }

    IReceiveEndpointConfigurator? InstanceEndpointConfigurator { get; }
}
