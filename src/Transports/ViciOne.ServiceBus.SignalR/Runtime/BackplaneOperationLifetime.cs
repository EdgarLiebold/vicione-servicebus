using System.Runtime.ExceptionServices;

namespace ViciOne.ServiceBus.SignalR.Runtime;

/// <summary>Releases backplane operation owners without losing operation or release failures.</summary>
internal static class BackplaneOperationLifetime
{
    /// <summary>Joins every started publication even when admitting a later publication fails synchronously.</summary>
    public static async Task JoinStartedOperationsAsync(IEnumerable<Task> operations)
    {
        var started = new List<Task>();
        Exception? admissionFailure = null;
        try
        {
            foreach (Task operation in operations)
            {
                if (operation is null)
                    throw new ArgumentException("The task sequence included a null task.", nameof(operations));

                started.Add(operation);
            }
        }
        catch (Exception exception)
        {
            admissionFailure = exception;
        }

        Task completion = Task.WhenAll(started);
        try
        {
            await completion.ConfigureAwait(false);
        }
        catch (Exception completionFailure)
        {
            if (admissionFailure is null)
                throw;

            var failures = new List<Exception> { admissionFailure };
            if (completion.Exception is { } completionFailures)
                failures.AddRange(completionFailures.InnerExceptions);
            else
                failures.Add(completionFailure);

            throw new AggregateException("Backplane publication admission and completion encountered multiple failures.", failures);
        }

        if (admissionFailure is not null)
            ExceptionDispatchInfo.Capture(admissionFailure).Throw();
    }

    /// <summary>Releases the request before the scope and propagates every selected outcome.</summary>
    public static async Task ReleaseAfterOperationAsync(
        IAsyncDisposable scope,
        IDisposable? request,
        Exception? operationFailure)
    {
        List<Exception>? failures = operationFailure is null ? null : [operationFailure];

        try
        {
            request?.Dispose();
        }
        catch (Exception exception)
        {
            (failures ??= []).Add(exception);
        }

        try
        {
            await scope.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            (failures ??= []).Add(exception);
        }

        if (failures is { Count: 1 })
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures is { Count: > 1 })
            throw new AggregateException("Backplane operation and release encountered multiple failures.", failures);
    }
}
