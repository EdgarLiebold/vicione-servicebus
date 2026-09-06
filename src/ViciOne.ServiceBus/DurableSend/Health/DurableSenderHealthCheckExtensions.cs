using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ViciOne.ServiceBus;


namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for durable sender health check.
/// </summary>
public static class DurableSenderHealthCheckExtensions
{
    /// <summary>
    /// Registers one typed durable-sender health check. The host still owns endpoint exposure, authorization,
    /// response formatting and external monitoring policy.
    /// </summary>
    public static IHealthChecksBuilder AddViciOneReliableMessagingHealthCheck<TBus>(
        this IHealthChecksBuilder builder,
        string? name = null,
        IEnumerable<string>? tags = null)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(builder);

        string registrationName = name ?? $"vicione-servicebus-durable-sender:{typeof(TBus).FullName ?? typeof(TBus).Name}";
        return builder.AddCheck<DurableSenderHealthCheck<TBus>>(
            registrationName,
            failureStatus: HealthStatus.Unhealthy,
            tags: tags ?? ["ready", "messaging", "durable-sender"]);
    }
}
