using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Represents an endpoint health result value.
/// </summary>
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

    /// <summary>
    /// Defines the input address value.
    /// </summary>
    public readonly Uri InputAddress;

    /// <summary>
    /// Defines the status value.
    /// </summary>
    public readonly BusHealthStatus Status;

    /// <summary>
    /// Defines the description value.
    /// </summary>
    public readonly string? Description;

    /// <summary>
    /// Defines the exception value.
    /// </summary>
    public readonly Exception? Exception;

    /// <summary>
    /// Defines the receive endpoint value.
    /// </summary>
    public readonly IReceiveEndpoint ReceiveEndpoint;

    /// <summary>
    /// Performs the healthy operation.
    /// </summary>
    /// <param name="receiveEndpoint">The receive endpoint value.</param>
    /// <param name="description">The description value.</param>
    /// <returns>The result of the operation.</returns>
    public static EndpointHealthResult Healthy(IReceiveEndpoint receiveEndpoint, string? description)
    {
        return new EndpointHealthResult(BusHealthStatus.Healthy, receiveEndpoint, description);
    }

    /// <summary>
    /// Performs the degraded operation.
    /// </summary>
    /// <param name="receiveEndpoint">The receive endpoint value.</param>
    /// <param name="description">The description value.</param>
    /// <returns>The result of the operation.</returns>
    public static EndpointHealthResult Degraded(IReceiveEndpoint receiveEndpoint, string? description)
    {
        return new EndpointHealthResult(BusHealthStatus.Degraded, receiveEndpoint, description);
    }

    /// <summary>
    /// Performs the unhealthy operation.
    /// </summary>
    /// <param name="receiveEndpoint">The receive endpoint value.</param>
    /// <param name="description">The description value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static EndpointHealthResult Unhealthy(IReceiveEndpoint receiveEndpoint, string? description, Exception? exception)
    {
        return new EndpointHealthResult(BusHealthStatus.Unhealthy, receiveEndpoint, description, exception);
    }
}
