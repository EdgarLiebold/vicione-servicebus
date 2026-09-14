using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Consumers;

/// <summary>Completes an owned consumer operation and releases the consumer without losing either failure.</summary>
internal static class OwnedConsumerLifetime
{
    /// <summary>Releases the consumer and propagates the operation and release outcomes.</summary>
    /// <param name="consumer">The owned consumer, or <see langword="null" /> when creation failed.</param>
    /// <param name="operationFailure">The failure selected by consumer creation or pipeline execution.</param>
    /// <returns>A task that completes after consumer release and outcome propagation.</returns>
    public static async Task ReleaseAfterOperationAsync(object? consumer, Exception? operationFailure)
    {
        Exception? releaseFailure = null;
        try
        {
            switch (consumer)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
        catch (Exception exception)
        {
            releaseFailure = exception;
        }

        if (operationFailure is not null && releaseFailure is not null)
        {
            throw new AggregateException(
                "Consumer pipeline and release encountered multiple failures.",
                operationFailure,
                releaseFailure);
        }

        if (operationFailure is not null)
            ExceptionDispatchInfo.Capture(operationFailure).Throw();
        if (releaseFailure is not null)
            ExceptionDispatchInfo.Capture(releaseFailure).Throw();
    }
}
