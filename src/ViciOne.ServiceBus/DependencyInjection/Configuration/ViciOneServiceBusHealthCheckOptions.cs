using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Stores the .NET health-check registration settings for one bus contract.</summary>
/// <typeparam name="TBus">The bus contract that owns the health check.</typeparam>
public sealed class ViciOneServiceBusHealthCheckOptions<TBus> :
    IHealthCheckOptionsConfigurator,
    IHealthCheckOptions
    where TBus : IBus
{
    readonly HashSet<string> _tags = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Initializes an empty option set that uses the bus-derived registration defaults.</summary>
    public ViciOneServiceBusHealthCheckOptions()
    {
    }

    /// <summary>Gets or sets an optional registration name that replaces the bus-derived default.</summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the lowest <see cref="HealthStatus"/> value the registration reports. A missing value
    /// preserves the bus snapshot status without applying a floor.
    /// </summary>
    public HealthStatus? MinimalFailureStatus { get; set; }

    /// <summary>
    /// Gets the mutable, case-insensitive registration tags. An empty set selects the built-in readiness tags.
    /// </summary>
    public ISet<string> Tags => _tags;

    IReadOnlySet<string> IHealthCheckOptions.Tags => _tags;
}
