using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides lifecycle operations with cooperative cancellation for bus-control instances.</summary>
public static class BusControlExtensions
{
    /// <summary>Starts a bus and requests cancellation when the specified timeout elapses.</summary>
    /// <param name="bus">The bus to start.</param>
    /// <param name="startTimeout">The positive delay before requesting startup cancellation.</param>
    /// <param name="timeProvider">The clock used to measure the timeout, or the system clock when omitted.</param>
    /// <param name="cancellationToken">Requests startup cancellation independently of the timeout.</param>
    /// <returns>A task that completes when the bus has started.</returns>
    /// <remarks>Waits for the bus startup task to finish. Cancellation requires cooperation from the bus and does not impose a hard completion deadline.</remarks>
    public static async Task StartAsync(this IBusControl bus, TimeSpan startTimeout, TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bus);
        if (startTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(startTimeout), startTimeout, "The startup timeout must be positive.");

        cancellationToken.ThrowIfCancellationRequested();
        using var timeoutTokenSource = new CancellationTokenSource(startTimeout, timeProvider ?? TimeProvider.System);
        using var linkedTokenSource = cancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token)
            : null;

        try
        {
            await bus.StartAsync(linkedTokenSource?.Token ?? timeoutTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(exception.Message, exception, cancellationToken);
        }
    }

    /// <summary>Stops a bus and requests cancellation when the specified timeout elapses.</summary>
    /// <param name="bus">The bus to stop.</param>
    /// <param name="stopTimeout">The positive delay before requesting shutdown cancellation.</param>
    /// <param name="timeProvider">The clock used to measure the timeout, or the system clock when omitted.</param>
    /// <param name="cancellationToken">Requests shutdown cancellation independently of the timeout.</param>
    /// <returns>A task that completes when the bus has stopped.</returns>
    /// <remarks>Waits for the bus shutdown task to finish. Cancellation requires cooperation from the bus and does not impose a hard completion deadline.</remarks>
    public static async Task StopAsync(this IBusControl bus, TimeSpan stopTimeout, TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bus);
        if (stopTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(stopTimeout), stopTimeout, "The shutdown timeout must be positive.");

        cancellationToken.ThrowIfCancellationRequested();
        using var timeoutTokenSource = new CancellationTokenSource(stopTimeout, timeProvider ?? TimeProvider.System);
        using var linkedTokenSource = cancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token)
            : null;

        try
        {
            await bus.StopAsync(linkedTokenSource?.Token ?? timeoutTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(exception.Message, exception, cancellationToken);
        }
    }

    /// <summary>Starts a bus to deploy its topology, then stops it after startup completes.</summary>
    /// <param name="bus">The bus whose topology is deployed.</param>
    /// <param name="cancellationToken">Cancels startup.</param>
    /// <returns>A task that completes after topology deployment and shutdown.</returns>
    /// <remarks>Configure topology-only deployment on the bus before calling this method when consumption must be disabled. This method itself does not disable consumption.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null" />.</exception>
    public static async Task DeployAsync(this IBusControl bus, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bus);

        await bus.StartAsync(cancellationToken).ConfigureAwait(false);

        await bus.StopAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
