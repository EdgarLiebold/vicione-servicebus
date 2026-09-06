using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides extension methods for bus control.
/// </summary>
public static class BusControlExtensions
{
    /// <summary>
    /// Stop a bus, throwing an exception if the bus does not stop.
    /// It is a wrapper of the async method `StopAsync`
    /// </summary>
    /// <param name="busControl">The bus handle</param>
    public static void Stop(this IBusControl busControl)
    {
        Stop(busControl, TimeSpan.FromSeconds(60));
    }

    /// <summary>
    /// Starts a bus, throwing an exception if the bus does not start
    /// It is a wrapper of the async method `StartAsync`
    /// </summary>
    /// <param name="busControl">The bus handle</param>
    public static void Start(this IBusControl busControl)
    {
        Start(busControl, TimeSpan.FromSeconds(60));
    }

    /// <summary>
    /// Stop a bus, throwing an exception if the bus does not stop in the specified timeout
    /// </summary>
    /// <param name="bus">The bus handle</param>
    /// <param name="stopTimeout">The wait time before throwing an exception</param>
    public static void Stop(this IBusControl bus, TimeSpan stopTimeout)
    {
        using var cancellationTokenSource = new CancellationTokenSource(stopTimeout);

        var cancellationToken = cancellationTokenSource.Token;

        TaskBlocking.Wait(() => bus.StopAsync(cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Start a bus, throwing an exception if the bus does not start in the specified timeout
    /// </summary>
    /// <param name="bus">The bus handle</param>
    /// <param name="startTimeout">The wait time before throwing an exception</param>
    public static void Start(this IBusControl bus, TimeSpan startTimeout)
    {
        using var cancellationTokenSource = new CancellationTokenSource(startTimeout);

        TaskBlocking.Wait(() => bus.StartAsync(cancellationTokenSource.Token), cancellationTokenSource.Token);
    }

    /// <summary>
    /// Start a bus, throwing an exception if the bus does not start in the specified timeout
    /// </summary>
    /// <param name="bus">The bus handle</param>
    /// <param name="startTimeout">The wait time before throwing an exception</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task StartAsync(this IBusControl bus, TimeSpan startTimeout, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var timeoutTokenSource = new CancellationTokenSource(startTimeout);
        using var linkedTokenSource = cancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token)
            : null;

        await bus.StartAsync(linkedTokenSource?.Token ?? timeoutTokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Stop a bus, throwing an exception if the bus does not stop in the specified timeout
    /// </summary>
    /// <param name="bus">The bus handle</param>
    /// <param name="stopTimeout">The wait time before throwing an exception</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task StopAsync(this IBusControl bus, TimeSpan stopTimeout, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var timeoutTokenSource = new CancellationTokenSource(stopTimeout);
        using var linkedTokenSource = cancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token)
            : null;

        await bus.StopAsync(linkedTokenSource?.Token ?? timeoutTokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// This can be used to start and stop the bus when configured in a deploy topology only scenario. No messages should be consumed by it.
    /// </summary>
    /// <param name="bus"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static async Task DeployAsync(this IBusControl bus, CancellationToken cancellationToken = default)
    {
        if (bus == null)
            throw new ArgumentNullException(nameof(bus));

        await bus.StartAsync(cancellationToken).ConfigureAwait(false);

        await bus.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}
