using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Operations.ReliableMessaging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers reliable-messaging readiness checks.</summary>
public static class DurableSenderHealthCheckExtensions
{
    /// <summary>
    /// Registers one typed durable-sender health check. The host still owns endpoint exposure, authorization,
    /// response formatting and external monitoring policy.
    /// </summary>
    /// <typeparam name="TBus">The bus whose durable store is observed.</typeparam>
    /// <param name="builder">The health-check registration builder.</param>
    /// <param name="name">An optional registration name; a bus-specific name is used when omitted.</param>
    /// <param name="tags">Optional health-check classification tags.</param>
    /// <returns><paramref name="builder" /> for continued registration.</returns>
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
