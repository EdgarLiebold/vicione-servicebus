using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Provides the runtime state and endpoint settings shared by job consumers in one service instance.</summary>
internal interface JobServiceSettings :
    IOptions
{
    /// <summary>Gets the job runtime that owns active jobs for the service instance.</summary>
    IJobService Runtime { get; }

    /// <summary>Gets the interval between service-instance heartbeat publications.</summary>
    TimeSpan HeartbeatInterval { get; }

    /// <summary>Gets the delay applied before a locally rejected job is retried.</summary>
    TimeSpan RejectedJobDelay { get; }

    /// <summary>Gets the clock used by job runtime timers and timestamps.</summary>
    TimeProvider TimeProvider { get; }

    /// <summary>Gets the input address of the configured service-instance endpoint.</summary>
    Uri? InstanceAddress { get; }

    /// <summary>Gets the configured service-instance endpoint.</summary>
    IReceiveEndpointConfigurator? InstanceEndpoint { get; }
}
