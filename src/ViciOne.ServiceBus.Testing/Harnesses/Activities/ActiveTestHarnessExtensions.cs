using System;
using System.Runtime.ExceptionServices;
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
    /// An action, setup or wait failure is preserved while all owned cleanup is attempted.
    /// After a successful action and wait, multiple owner cleanup failures are aggregated in disposal order;
    /// an observation owner's aggregate remains one inner owner failure.
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

        var activity = new TrackedActivity(operationName ?? "test act", harness.TestTimeout, harness.TestInactivityTimeout,
            harness.TimeProvider);
        ActiveTestObservationScope? observations = null;
        bool bodyCompleted = false;
        try
        {
            observations = new ActiveTestObservationScope(harness, harness.TimeProvider, activity.TraceId);
            await action().ConfigureAwait(false);
            activity.ActionCompleted();
            await activity.WaitForCompletionAsync(cancellationToken).ConfigureAwait(false);
            ActiveTestResult result = observations.Snapshot();
            bodyCompleted = true;
            return result;
        }
        catch
        {
            activity.StopWaiting();
            throw;
        }
        finally
        {
            DisposeOwners(observations, activity, bodyCompleted);
        }
    }

    static void DisposeOwners(ActiveTestObservationScope? observations, TrackedActivity activity, bool bodyCompleted)
    {
        Exception? observationFailure = null;
        try
        {
            observations?.Dispose();
        }
        catch (Exception exception)
        {
            observationFailure = exception;
        }

        try
        {
            activity.Dispose();
        }
        catch (Exception exception)
        {
            if (bodyCompleted)
            {
                if (observationFailure is not null)
                    throw new AggregateException("Active test observation and tracker cleanup both failed.", observationFailure, exception);
                throw;
            }
        }

        if (bodyCompleted && observationFailure is not null)
            ExceptionDispatchInfo.Capture(observationFailure).Throw();
    }
}
