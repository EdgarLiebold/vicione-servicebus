using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Represents the outcome of bus health.</summary>
public class BusHealthResult
{
    /// <summary>Exposes the endpoints used by the containing type.</summary>
    public readonly IReadOnlyDictionary<string, EndpointHealthResult> Endpoints;

    /// <summary>Exposes the exception used by the containing type.</summary>
    public readonly Exception? Exception;

    /// <summary>Exposes the status used by the containing type.</summary>
    public readonly BusHealthStatus Status;

    BusHealthResult(BusHealthStatus status, string description, Exception? exception, IReadOnlyDictionary<string, EndpointHealthResult> endpoints)
    {
        Status = status;
        Description = description;
        Exception = exception;
        Endpoints = endpoints;
    }

    /// <summary>Gets the description.</summary>
    public string Description { get; }

    /// <summary>Creates a healthy status result.</summary>
    /// <param name="description">The description.</param>
    /// <param name="endpoints">The endpoints.</param>
    /// <returns>The bus health result produced by the operation.</returns>
    public static BusHealthResult Healthy(string description, IReadOnlyDictionary<string, EndpointHealthResult> endpoints)
    {
        return new BusHealthResult(BusHealthStatus.Healthy, description, null, endpoints);
    }

    /// <summary>Creates a degraded health result.</summary>
    /// <param name="description">The description.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="endpoints">The endpoints.</param>
    /// <returns>The bus health result produced by the operation.</returns>
    public static BusHealthResult Degraded(string description, Exception? exception, IReadOnlyDictionary<string, EndpointHealthResult> endpoints)
    {
        return new BusHealthResult(BusHealthStatus.Degraded, description, exception, endpoints);
    }

    /// <summary>Creates an unhealthy status result.</summary>
    /// <param name="description">The description.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="endpoints">The endpoints.</param>
    /// <returns>The bus health result produced by the operation.</returns>
    public static BusHealthResult Unhealthy(string description, Exception? exception, IReadOnlyDictionary<string, EndpointHealthResult> endpoints)
    {
        return new BusHealthResult(BusHealthStatus.Unhealthy, description, exception, endpoints);
    }
}
