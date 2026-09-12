namespace ViciOne.ServiceBus;

/// <summary>Indicates that a message body exceeded its bus-owned hard limit.</summary>
public sealed class MessageTooLargeException : ViciOneServiceBusException
{
    /// <summary>Creates a rejection for an oversized message body.</summary>
    /// <param name="actualBytes">The message-body length observed at the enforcing boundary.</param>
    /// <param name="maximumBytes">The configured inclusive byte limit.</param>
    /// <param name="endpointAddress">The endpoint that rejected the oversized body.</param>
    public MessageTooLargeException(long actualBytes, long maximumBytes, Uri endpointAddress)
        : base(CreateMessage(actualBytes, maximumBytes, endpointAddress))
    {
        ActualBytes = actualBytes;
        MaximumBytes = maximumBytes;
        EndpointAddress = endpointAddress;
    }

    /// <summary>Gets the observed message-body length.</summary>
    public long ActualBytes { get; }

    /// <summary>Gets the configured inclusive byte limit.</summary>
    public long MaximumBytes { get; }

    /// <summary>Gets the endpoint address that rejected the body.</summary>
    public Uri EndpointAddress { get; }

    private static string CreateMessage(long actualBytes, long maximumBytes, Uri endpointAddress)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(actualBytes);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumBytes, 1);
        ArgumentNullException.ThrowIfNull(endpointAddress);

        if (actualBytes <= maximumBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actualBytes),
                actualBytes,
                $"Must exceed {nameof(maximumBytes)} ({maximumBytes}) for an oversized message.");
        }

        return $"Message body contains {actualBytes} bytes and exceeds the configured limit of {maximumBytes} bytes at '{endpointAddress}'.";
    }
}
