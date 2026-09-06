using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Represents an endpoint health result.</summary>
public readonly struct EndpointHealthResult
{
    EndpointHealthResult(BusHealthStatus status, IReceiveEndpoint receiveEndpoint, string? description = null, Exception? exception = null)
    {
        Status = status;
        ReceiveEndpoint = receiveEndpoint;
        InputAddress = receiveEndpoint.InputAddress;
        Description = description;
        Exception = exception;
    }

    /// <summary>Exposes the input address used by the containing type.</summary>
    public readonly Uri InputAddress;

    /// <summary>Exposes the status used by the containing type.</summary>
    public readonly BusHealthStatus Status;

    /// <summary>Exposes the description used by the containing type.</summary>
    public readonly string? Description;

    /// <summary>Exposes the exception used by the containing type.</summary>
    public readonly Exception? Exception;

    /// <summary>Exposes the receive endpoint used by the containing type.</summary>
    public readonly IReceiveEndpoint ReceiveEndpoint;

    /// <summary>Creates a healthy status result.</summary>
    /// <param name="receiveEndpoint">The receive endpoint.</param>
    /// <param name="description">The description.</param>
    /// <returns>The endpoint health result produced by the operation.</returns>
    public static EndpointHealthResult Healthy(IReceiveEndpoint receiveEndpoint, string? description)
    {
        return new EndpointHealthResult(BusHealthStatus.Healthy, receiveEndpoint, description);
    }

    /// <summary>Creates a degraded health result.</summary>
    /// <param name="receiveEndpoint">The receive endpoint.</param>
    /// <param name="description">The description.</param>
    /// <returns>The endpoint health result produced by the operation.</returns>
    public static EndpointHealthResult Degraded(IReceiveEndpoint receiveEndpoint, string? description)
    {
        return new EndpointHealthResult(BusHealthStatus.Degraded, receiveEndpoint, description);
    }

    /// <summary>Creates an unhealthy status result.</summary>
    /// <param name="receiveEndpoint">The receive endpoint.</param>
    /// <param name="description">The description.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The endpoint health result produced by the operation.</returns>
    public static EndpointHealthResult Unhealthy(IReceiveEndpoint receiveEndpoint, string? description, Exception? exception)
    {
        return new EndpointHealthResult(BusHealthStatus.Unhealthy, receiveEndpoint, description, exception);
    }
}
