using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Provides extension methods for active test harness.</summary>
public static class ActiveTestHarnessExtensions
{
    /// <summary>
    /// Executes one test action, captures messages produced while that action and its resulting activity chain are active,
    /// and completes only after the chain becomes idle or the harness timeout expires.
    /// The capture is independent from the harness' historical context save mode.
    /// </summary>
    /// <param name="harness">The harness used by the operation.</param>
    /// <param name="action">The action used by the operation.</param>
    /// <param name="operationName">The operation name used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the act outcome.</returns>
    public static async Task<ActiveTestResult> ActAsync(this IBaseTestHarness harness, Func<Task> action, string? operationName = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); ArgumentNullException.ThrowIfNull(harness);
        ArgumentNullException.ThrowIfNull(action);

        await using var activity = new TrackedActivity(operationName ?? "test act", harness.TestTimeout, harness.TestInactivityTimeout,
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

        // The return expression of an await-using method is evaluated before DisposeAsync runs.
        // Waiting explicitly keeps the observers connected and delays the snapshot until every
        // causally linked activity has completed or the owned timeout has elapsed.
        await activity.WaitForCompletionAsync().ConfigureAwait(false);
        return observations.Snapshot();
    }
}
