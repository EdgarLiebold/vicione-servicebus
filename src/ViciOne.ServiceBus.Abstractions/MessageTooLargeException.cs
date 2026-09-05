namespace ViciOne.ServiceBus;

/// <summary>
/// Indicates that a received transport body exceeded its bus-owned hard limit.
/// </summary>
public sealed class MessageTooLargeException : Exception
{
    /// <summary>
    /// Initializes a receive-side size rejection.
    /// </summary>
    /// <param name="actualBytes">The transport-body length observed at the receive boundary.</param>
    /// <param name="maximumBytes">The configured maximum transport-body length.</param>
    /// <param name="inputAddress">The endpoint that received the oversized body.</param>
    public MessageTooLargeException(long actualBytes, long maximumBytes, Uri inputAddress)
        : base($"Received message body contains {actualBytes} bytes and exceeds the configured limit of {maximumBytes} bytes at '{inputAddress}'.")
    {
        ArgumentOutOfRangeException.ThrowIfNegative(actualBytes);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumBytes, 1);
        ArgumentNullException.ThrowIfNull(inputAddress);

        ActualBytes = actualBytes;
        MaximumBytes = maximumBytes;
        InputAddress = inputAddress;
    }

    /// <summary>
    /// Gets the transport-body length observed at the receive boundary.
    /// </summary>
    public long ActualBytes { get; }

    /// <summary>
    /// Gets the configured maximum transport-body length.
    /// </summary>
    public long MaximumBytes { get; }

    /// <summary>
    /// Gets the endpoint that received the oversized body.
    /// </summary>
    public Uri InputAddress { get; }
}
