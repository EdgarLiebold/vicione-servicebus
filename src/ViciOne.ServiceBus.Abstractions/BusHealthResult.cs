using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides a bus health result implementation.
/// </summary>
public class BusHealthResult
{
    /// <summary>
    /// Defines the endpoints value.
    /// </summary>
    public readonly IReadOnlyDictionary<string, EndpointHealthResult> Endpoints;

    /// <summary>
    /// Defines the exception value.
    /// </summary>
    public readonly Exception? Exception;

    /// <summary>
    /// Defines the status value.
    /// </summary>
    public readonly BusHealthStatus Status;

    BusHealthResult(BusHealthStatus status, string description, Exception? exception, IReadOnlyDictionary<string, EndpointHealthResult> endpoints)
    {
        Status = status;
        Description = description;
        Exception = exception;
        Endpoints = endpoints;
    }

    /// <summary>
    /// Gets the description value.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Performs the healthy operation.
    /// </summary>
    /// <param name="description">The description value.</param>
    /// <param name="endpoints">The endpoints value.</param>
    /// <returns>The result of the operation.</returns>
    public static BusHealthResult Healthy(string description, IReadOnlyDictionary<string, EndpointHealthResult> endpoints)
    {
        return new BusHealthResult(BusHealthStatus.Healthy, description, null, endpoints);
    }

    /// <summary>
    /// Performs the degraded operation.
    /// </summary>
    /// <param name="description">The description value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="endpoints">The endpoints value.</param>
    /// <returns>The result of the operation.</returns>
    public static BusHealthResult Degraded(string description, Exception? exception, IReadOnlyDictionary<string, EndpointHealthResult> endpoints)
    {
        return new BusHealthResult(BusHealthStatus.Degraded, description, exception, endpoints);
    }

    /// <summary>
    /// Performs the unhealthy operation.
    /// </summary>
    /// <param name="description">The description value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="endpoints">The endpoints value.</param>
    /// <returns>The result of the operation.</returns>
    public static BusHealthResult Unhealthy(string description, Exception? exception, IReadOnlyDictionary<string, EndpointHealthResult> endpoints)
    {
        return new BusHealthResult(BusHealthStatus.Unhealthy, description, exception, endpoints);
    }
}
