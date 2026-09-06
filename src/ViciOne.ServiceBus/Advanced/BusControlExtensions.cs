using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides bounded lifecycle operations for bus-control instances.</summary>
public static class BusControlExtensions
{
    /// <summary>Starts a bus and cancels the operation when the specified timeout elapses.</summary>
    /// <param name="bus">The bus to start.</param>
    /// <param name="startTimeout">The positive maximum startup duration.</param>
    /// <param name="cancellationToken">Cancels startup independently of the timeout.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task StartAsync(this IBusControl bus, TimeSpan startTimeout, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bus);
        if (startTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(startTimeout), startTimeout, "The startup timeout must be positive.");

        cancellationToken.ThrowIfCancellationRequested();
        using var timeoutTokenSource = new CancellationTokenSource(startTimeout);
        using var linkedTokenSource = cancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token)
            : null;

        await bus.StartAsync(linkedTokenSource?.Token ?? timeoutTokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Stops a bus and cancels the operation when the specified timeout elapses.</summary>
    /// <param name="bus">The bus to stop.</param>
    /// <param name="stopTimeout">The positive maximum shutdown duration.</param>
    /// <param name="cancellationToken">Cancels shutdown independently of the timeout.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task StopAsync(this IBusControl bus, TimeSpan stopTimeout, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bus);
        if (stopTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(stopTimeout), stopTimeout, "The shutdown timeout must be positive.");

        cancellationToken.ThrowIfCancellationRequested();
        using var timeoutTokenSource = new CancellationTokenSource(stopTimeout);
        using var linkedTokenSource = cancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token)
            : null;

        await bus.StopAsync(linkedTokenSource?.Token ?? timeoutTokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Starts and stops a bus to deploy its topology without running a message-consumption lifetime.</summary>
    /// <param name="bus">The bus whose topology is deployed.</param>
    /// <param name="cancellationToken">Cancels startup.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null" />.</exception>
    public static async Task DeployAsync(this IBusControl bus, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bus);

        await bus.StartAsync(cancellationToken).ConfigureAwait(false);

        await bus.StopAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
