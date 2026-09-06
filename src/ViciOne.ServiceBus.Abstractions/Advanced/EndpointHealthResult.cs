using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides one immutable observation of receive-endpoint health.</summary>
public sealed class EndpointHealthResult
{
    private EndpointHealthResult(
        BusHealthStatus status,
        IReceiveEndpoint receiveEndpoint,
        string? description = null,
        Exception? exception = null)
    {
        ArgumentNullException.ThrowIfNull(receiveEndpoint);

        Status = status;
        ReceiveEndpoint = receiveEndpoint;
        InputAddress = receiveEndpoint.InputAddress;
        Description = description;
        Exception = exception;
    }

    /// <summary>Gets the endpoint status.</summary>
    public BusHealthStatus Status { get; }

    /// <summary>Gets the receive endpoint that produced the observation.</summary>
    public IReceiveEndpoint ReceiveEndpoint { get; }

    /// <summary>Gets the endpoint input address captured with the observation.</summary>
    public Uri InputAddress { get; }

    /// <summary>Gets the diagnostic description, when available.</summary>
    public string? Description { get; }

    /// <summary>Gets the endpoint failure, when available.</summary>
    public Exception? Exception { get; }

    /// <summary>Creates a healthy status result.</summary>
    /// <param name="receiveEndpoint">The endpoint that produced the observation.</param>
    /// <param name="description">The diagnostic description, when available.</param>
    /// <returns>An immutable healthy endpoint observation.</returns>
    public static EndpointHealthResult Healthy(IReceiveEndpoint receiveEndpoint, string? description)
    {
        return new EndpointHealthResult(BusHealthStatus.Healthy, receiveEndpoint, description);
    }

    /// <summary>Creates a degraded health result.</summary>
    /// <param name="receiveEndpoint">The endpoint that produced the observation.</param>
    /// <param name="description">The diagnostic description, when available.</param>
    /// <returns>An immutable degraded endpoint observation.</returns>
    public static EndpointHealthResult Degraded(IReceiveEndpoint receiveEndpoint, string? description)
    {
        return new EndpointHealthResult(BusHealthStatus.Degraded, receiveEndpoint, description);
    }

    /// <summary>Creates an unhealthy status result.</summary>
    /// <param name="receiveEndpoint">The endpoint that produced the observation.</param>
    /// <param name="description">The diagnostic description, when available.</param>
    /// <param name="exception">The endpoint failure, when available.</param>
    /// <returns>An immutable unhealthy endpoint observation.</returns>
    public static EndpointHealthResult Unhealthy(IReceiveEndpoint receiveEndpoint, string? description, Exception? exception)
    {
        return new EndpointHealthResult(BusHealthStatus.Unhealthy, receiveEndpoint, description, exception);
    }
}
