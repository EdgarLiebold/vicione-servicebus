using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;

#nullable enable
namespace ViciOne.ServiceBus.Configuration;

public class ViciOneServiceBusHealthCheckOptions<TBus> :
    IHealthCheckOptionsConfigurator,
    IHealthCheckOptions
    where TBus : IBus
{
    public ViciOneServiceBusHealthCheckOptions()
    {
        Tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The health check name. If null the type name of bus instance will be used
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The minimal <see cref="HealthStatus" /> that should be reported when the health check fails.
    /// If null then all statuses from <see cref="HealthStatus.Unhealthy"/> to <see cref="HealthStatus.Healthy"/> will be reported depending on app health.
    /// </summary>
    public HealthStatus? MinimalFailureStatus { get; set; }

    /// <summary>
    /// A list of tags that can be used to filter sets of health checks. If empty, the default tags
    /// will be used.
    /// </summary>
    public HashSet<string> Tags { get; }
}
