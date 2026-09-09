using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures the .NET health-check registration associated with one bus.</summary>
public interface IHealthCheckOptionsConfigurator
{
    /// <summary>Sets an optional registration name that replaces the bus-derived default.</summary>
    public string? Name { set; }

    /// <summary>
    /// Sets the lowest <see cref="HealthStatus"/> value the registration reports. A missing value preserves
    /// the bus snapshot status without applying a floor.
    /// </summary>
    public HealthStatus? MinimalFailureStatus { set; }

    /// <summary>Gets the mutable, case-insensitive tag set used to classify the registration.</summary>
    public ISet<string> Tags { get; }
}
