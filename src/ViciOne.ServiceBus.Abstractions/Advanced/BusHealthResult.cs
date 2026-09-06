using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides one immutable snapshot of aggregate bus and receive-endpoint health.</summary>
public sealed class BusHealthResult
{
    private BusHealthResult(
        BusHealthStatus status,
        string description,
        Exception? exception,
        IReadOnlyDictionary<string, EndpointHealthResult> endpoints)
    {
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(endpoints);

        Status = status;
        Description = description;
        Exception = exception;
        Endpoints = endpoints.ToFrozenDictionary(StringComparer.Ordinal);
    }

    /// <summary>Gets the aggregate health status.</summary>
    public BusHealthStatus Status { get; }

    /// <summary>Gets the diagnostic description associated with the snapshot.</summary>
    public string Description { get; }

    /// <summary>Gets the failure that caused a degraded or unhealthy result, when available.</summary>
    public Exception? Exception { get; }

    /// <summary>Gets the endpoint observations keyed by endpoint identifier.</summary>
    public IReadOnlyDictionary<string, EndpointHealthResult> Endpoints { get; }

    /// <summary>Creates a healthy status result.</summary>
    /// <param name="description">The diagnostic description for the healthy bus.</param>
    /// <param name="endpoints">The endpoint observations to capture in the snapshot.</param>
    /// <returns>An immutable healthy bus snapshot.</returns>
    public static BusHealthResult Healthy(string description, IReadOnlyDictionary<string, EndpointHealthResult> endpoints)
    {
        return new BusHealthResult(BusHealthStatus.Healthy, description, null, endpoints);
    }

    /// <summary>Creates a degraded health result.</summary>
    /// <param name="description">The diagnostic description for the degraded bus.</param>
    /// <param name="exception">The failure that caused degradation, when available.</param>
    /// <param name="endpoints">The endpoint observations to capture in the snapshot.</param>
    /// <returns>An immutable degraded bus snapshot.</returns>
    public static BusHealthResult Degraded(string description, Exception? exception, IReadOnlyDictionary<string, EndpointHealthResult> endpoints)
    {
        return new BusHealthResult(BusHealthStatus.Degraded, description, exception, endpoints);
    }

    /// <summary>Creates an unhealthy status result.</summary>
    /// <param name="description">The diagnostic description for the unhealthy bus.</param>
    /// <param name="exception">The failure that caused the unhealthy result, when available.</param>
    /// <param name="endpoints">The endpoint observations to capture in the snapshot.</param>
    /// <returns>An immutable unhealthy bus snapshot.</returns>
    public static BusHealthResult Unhealthy(string description, Exception? exception, IReadOnlyDictionary<string, EndpointHealthResult> endpoints)
    {
        return new BusHealthResult(BusHealthStatus.Unhealthy, description, exception, endpoints);
    }
}
