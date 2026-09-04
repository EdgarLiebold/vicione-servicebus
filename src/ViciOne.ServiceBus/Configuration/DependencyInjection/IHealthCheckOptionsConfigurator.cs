using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for health check options configurator.
/// </summary>
public interface IHealthCheckOptionsConfigurator
{
    /// <summary>
    /// Set the health check name, overrides the default bus type name
    /// </summary>
    public string Name { set; }

    /// <summary>
    /// The minimal <see cref="HealthStatus" /> that should be reported when the health check fails.
    /// If null then all statuses from <see cref="HealthStatus.Unhealthy"/> to <see cref="HealthStatus.Healthy"/> will be reported depending on app health.
    /// </summary>
    public HealthStatus? MinimalFailureStatus { set; }

    /// <summary>
    /// A list of tags that can be used to filter sets of health checks
    /// </summary>
    public HashSet<string> Tags { get; }
}
