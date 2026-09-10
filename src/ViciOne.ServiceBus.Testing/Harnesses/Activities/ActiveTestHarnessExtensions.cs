using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Internal;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Captures message activity caused by a bounded test action.</summary>
public static class ActiveTestHarnessExtensions
{
    /// <summary>
    /// Executes one test action, captures messages produced while that action and its resulting activity chain are active,
    /// and completes only after the chain becomes idle or the harness timeout expires.
    /// The capture is independent from the harness' historical context save mode.
    /// </summary>
    /// <param name="harness">The harness that supplies observations and timeout settings.</param>
    /// <param name="action">The operation whose causal message activity is captured.</param>
    /// <param name="operationName">The diagnostic operation name.</param>
    /// <param name="cancellationToken">The token that cancels completion tracking.</param>
    /// <returns>A task whose result contains the captured message observations.</returns>
    public static async Task<ActiveTestResult> ActAsync(this IBaseTestHarness harness, Func<Task> action,
        string? operationName = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(harness);
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();

        using var activity = new TrackedActivity(operationName ?? "test act", harness.TestTimeout, harness.TestInactivityTimeout,
            harness.TimeProvider);
        using var observations = new ActiveTestObservationScope(harness, harness.TimeProvider, activity.TraceId);

        try
        {
            await action().ConfigureAwait(false);
            activity.ActionCompleted();
        }
        catch
        {
            activity.StopWaiting();
            throw;
        }

        try
        {
            await activity.WaitForCompletionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            activity.StopWaiting();
            throw;
        }

        return observations.Snapshot();
    }
}
