using System;

namespace ViciOne.ServiceBus.Advanced;
/// <summary>The requested bus health status was not reached within the configured timeout.</summary>
public sealed class BusHealthStatusTimeoutException : TimeoutException
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="expectedStatus">The expected status.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="lastResult">The last result.</param>
    public BusHealthStatusTimeoutException(
        BusHealthStatus expectedStatus,
        TimeSpan timeout,
        BusHealthResult lastResult)
        : base(CreateMessage(expectedStatus, timeout, lastResult))
    {
        ExpectedStatus = expectedStatus;
        Timeout = timeout;
        LastResult = lastResult;
    }

    /// <summary>Gets the expected status.</summary>
    public BusHealthStatus ExpectedStatus { get; }

    /// <summary>Gets the actual status.</summary>
    public BusHealthStatus ActualStatus => LastResult.Status;

    /// <summary>Gets the timeout.</summary>
    public TimeSpan Timeout { get; }

    /// <summary>Gets the last result.</summary>
    public BusHealthResult LastResult { get; }

    private static string CreateMessage(
        BusHealthStatus expectedStatus,
        TimeSpan timeout,
        BusHealthResult lastResult)
    {
        ArgumentNullException.ThrowIfNull(lastResult);

        return $"The bus did not reach health status '{expectedStatus}' within {timeout}. "
            + $"The last status was '{lastResult.Status}': {lastResult.Description}";
    }
}
