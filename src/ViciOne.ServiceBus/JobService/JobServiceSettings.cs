using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Settings relevant to the job consumer endpoints and the service instance.</summary>
public interface JobServiceSettings :
    IOptions
{
    /// <summary>Gets the job service.</summary>
    IJobService JobService { get; }

    /// <summary>Gets the heartbeat interval.</summary>
    TimeSpan HeartbeatInterval { get; }

    /// <summary>Adjust the time delay before a rejected job is retried.</summary>
    TimeSpan RejectedJobDelay { get; }

    /// <summary>Gets the time provider.</summary>
    TimeProvider TimeProvider { get; }

    /// <summary>Gets the instance address.</summary>
    Uri? InstanceAddress { get; }

    /// <summary>Gets the instance endpoint configurator.</summary>
    IReceiveEndpointConfigurator? InstanceEndpointConfigurator { get; }
}
