namespace ViciOne.ServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Waits for one or more bus controls to reach a specific health status.
/// </summary>
public static class BusControlHealthExtensions
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Waits until the bus reaches <paramref name="expectedStatus" /> or the timeout expires.
    /// </summary>
    public static Task<BusHealthResult> WaitForHealthStatusAsync(
        this IBusControl busControl,
        BusHealthStatus expectedStatus,
        TimeSpan timeout,
        CancellationToken cancellationToken = default) =>
        busControl.WaitForHealthStatusAsync(expectedStatus, timeout, TimeProvider.System, cancellationToken);

    /// <summary>
    /// Waits until the bus reaches <paramref name="expectedStatus" /> using the supplied time source.
    /// </summary>
    public static async Task<BusHealthResult> WaitForHealthStatusAsync(
        this IBusControl busControl,
        BusHealthStatus expectedStatus,
        TimeSpan timeout,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(busControl);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ValidateExpectedStatus(expectedStatus);
        ValidateTimeout(timeout);

        long startedAt = timeProvider.GetTimestamp();
        BusHealthResult? lastResult = null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TimeSpan elapsed = timeProvider.GetElapsedTime(startedAt);
            if (lastResult is not null
                && timeout != Timeout.InfiniteTimeSpan
                && elapsed > timeout)
            {
                throw new BusHealthStatusTimeoutException(expectedStatus, timeout, lastResult);
            }

            BusHealthResult result = busControl.CheckHealth();
            if (result.Status == expectedStatus)
                return result;

            lastResult = result;

            TimeSpan delay = PollInterval;
            if (timeout != Timeout.InfiniteTimeSpan)
            {
                TimeSpan remaining = timeout - elapsed;
                if (remaining <= TimeSpan.Zero)
                    throw new BusHealthStatusTimeoutException(expectedStatus, timeout, result);

                if (remaining < delay)
                    delay = remaining;
            }

            await Task.Delay(delay, timeProvider, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Waits until the bus reaches <paramref name="expectedStatus" /> or cancellation is requested.
    /// </summary>
    public static Task<BusHealthResult> WaitForHealthStatusAsync(
        this IBusControl busControl,
        BusHealthStatus expectedStatus,
        CancellationToken cancellationToken) =>
        busControl.WaitForHealthStatusAsync(
            expectedStatus,
            Timeout.InfiniteTimeSpan,
            TimeProvider.System,
            cancellationToken);

    /// <summary>
    /// Waits concurrently for every bus to reach <paramref name="expectedStatus" />.
    /// </summary>
    public static Task<BusHealthResult[]> WaitForHealthStatusAsync(
        this IEnumerable<IBusControl> busControls,
        BusHealthStatus expectedStatus,
        TimeSpan timeout,
        CancellationToken cancellationToken = default) =>
        busControls.WaitForHealthStatusAsync(
            expectedStatus,
            timeout,
            TimeProvider.System,
            cancellationToken);

    /// <summary>
    /// Waits concurrently for every bus to reach <paramref name="expectedStatus" /> using the
    /// supplied time source and returns results in input order.
    /// </summary>
    public static Task<BusHealthResult[]> WaitForHealthStatusAsync(
        this IEnumerable<IBusControl> busControls,
        BusHealthStatus expectedStatus,
        TimeSpan timeout,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(busControls);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ValidateExpectedStatus(expectedStatus);
        ValidateTimeout(timeout);

        IBusControl[] controls = busControls.ToArray();
        if (Array.Exists(controls, static control => control is null))
            throw new ArgumentException("The collection must not contain null bus controls.", nameof(busControls));

        return Task.WhenAll(controls.Select(control =>
            control.WaitForHealthStatusAsync(expectedStatus, timeout, timeProvider, cancellationToken)));
    }

    /// <summary>
    /// Waits concurrently for every bus to reach <paramref name="expectedStatus" /> or cancellation
    /// is requested.
    /// </summary>
    public static Task<BusHealthResult[]> WaitForHealthStatusAsync(
        this IEnumerable<IBusControl> busControls,
        BusHealthStatus expectedStatus,
        CancellationToken cancellationToken) =>
        busControls.WaitForHealthStatusAsync(
            expectedStatus,
            Timeout.InfiniteTimeSpan,
            TimeProvider.System,
            cancellationToken);

    private static void ValidateExpectedStatus(BusHealthStatus expectedStatus)
    {
        if (!Enum.IsDefined(expectedStatus))
            throw new ArgumentOutOfRangeException(nameof(expectedStatus), expectedStatus, "Must be a defined bus health status.");
    }

    private static void ValidateTimeout(TimeSpan timeout)
    {
        if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Must be non-negative or infinite.");
    }
}
