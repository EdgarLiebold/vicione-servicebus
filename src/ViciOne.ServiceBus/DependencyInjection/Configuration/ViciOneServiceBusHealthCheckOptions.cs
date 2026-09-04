using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;

#nullable enable
namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines configuration options for vici one service bus health check.
/// </summary>
/// <typeparam name="TBus">The t bus type.</typeparam>
public class ViciOneServiceBusHealthCheckOptions<TBus> :
    IHealthCheckOptionsConfigurator,
    IHealthCheckOptions
    where TBus : IBus
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
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
