namespace ViciOne.ServiceBus;

using System;

/// <summary>
/// The requested bus health status was not reached within the configured timeout.
/// </summary>
public sealed class BusHealthStatusTimeoutException : TimeoutException
{
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

    public BusHealthStatus ExpectedStatus { get; }

    public BusHealthStatus ActualStatus => LastResult.Status;

    public TimeSpan Timeout { get; }

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
