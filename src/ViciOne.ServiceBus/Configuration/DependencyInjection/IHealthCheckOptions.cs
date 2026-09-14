using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides the immutable read contract consumed while health-check registrations are projected.</summary>
internal interface IHealthCheckOptions
{
    /// <summary>Gets the optional explicit registration name.</summary>
    string? Name { get; }

    /// <summary>
    /// Gets the lowest <see cref="HealthStatus"/> value the registration reports, or <see langword="null"/>
    /// when the bus snapshot status is preserved unchanged.
    /// </summary>
    HealthStatus? MinimalFailureStatus { get; }

    /// <summary>Gets the configured registration tags.</summary>
    IReadOnlySet<string> Tags { get; }
}
