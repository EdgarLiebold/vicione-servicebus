namespace ViciOne.ServiceBus;

/// <summary>Indicates that a received transport body exceeded its bus-owned hard limit.</summary>
public sealed class MessageTooLargeException : Exception
{
    /// <summary>Initializes a receive-side size rejection.</summary>
    /// <param name="actualBytes">The transport-body length observed at the receive boundary.</param>
    /// <param name="maximumBytes">The configured inclusive byte limit.</param>
    /// <param name="inputAddress">The endpoint that received the oversized body.</param>
    public MessageTooLargeException(long actualBytes, long maximumBytes, Uri inputAddress)
        : base(CreateMessage(actualBytes, maximumBytes, inputAddress))
    {
        ActualBytes = actualBytes;
        MaximumBytes = maximumBytes;
        InputAddress = inputAddress;
    }

    /// <summary>Gets the observed transport-body length.</summary>
    public long ActualBytes { get; }

    /// <summary>Gets the configured inclusive byte limit.</summary>
    public long MaximumBytes { get; }

    /// <summary>Gets the input address that rejected the body.</summary>
    public Uri InputAddress { get; }

    private static string CreateMessage(long actualBytes, long maximumBytes, Uri inputAddress)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(actualBytes);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumBytes, 1);
        ArgumentNullException.ThrowIfNull(inputAddress);

        if (actualBytes <= maximumBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actualBytes),
                actualBytes,
                $"Must exceed {nameof(maximumBytes)} ({maximumBytes}) for an oversized message.");
        }

        return $"Received message body contains {actualBytes} bytes and exceeds the configured limit of {maximumBytes} bytes at '{inputAddress}'.";
    }
}
