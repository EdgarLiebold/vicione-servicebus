namespace ViciOne.ServiceBus;

/// <summary>Indicates that a received transport body exceeded its bus-owned hard limit.</summary>
public sealed class MessageTooLargeException : Exception
{
    /// <summary>Initializes a receive-side size rejection.</summary>
    /// <param name="actualBytes">The transport-body length observed at the receive boundary.</param>
    /// <param name="maximumBytes">The maximum bytes.</param>
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

    /// <summary>Gets the actual bytes.</summary>
    public long ActualBytes { get; }

    /// <summary>Gets the maximum bytes.</summary>
    public long MaximumBytes { get; }

    /// <summary>Gets the input address.</summary>
    public Uri InputAddress { get; }
}
