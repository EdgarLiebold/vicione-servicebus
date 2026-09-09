using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus;

internal sealed partial class ServiceBusRuntime
{
    /// <summary>
    /// Waits for the on-demand bus endpoint to become ready and surfaces a terminal transport failure
    /// instead of replacing it with the readiness timeout.
    /// </summary>
    void WaitUntilBusEndpointIsReady()
    {
        if (_busHandle == null || _receiveEndpoint.Started.IsCompletedSuccessfully)
            return;

        var terminal = _terminalFault;

        using var timeout = new CancellationTokenSource(ReadyTimeout, _timeProvider);

        terminal?.Attach(timeout);
        try
        {
            TaskBlocking.Wait(_receiveEndpoint.Started, timeout.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested || terminal?.Cause != null)
        {
            if (terminal?.Cause is { } cause)
                throw cause;

            throw new ConnectionException(
                $"The bus endpoint did not become ready within {ReadyTimeout.TotalSeconds:0} s, so the consumer "
                + $"could not be connected: {Address}");
        }
        finally
        {
            terminal?.Detach(timeout);
        }
    }

    /// <summary>
    /// Preserves the terminal bus-endpoint failure and cancels every registered readiness waiter.
    /// <para>
    /// Terminality is supplied by the receive transport's retry owner. Recoverable attempt faults leave
    /// waiters attached; retry exhaustion or another definitive startup failure wakes them with the
    /// original cause.
    /// </para>
    /// </summary>
    internal sealed class TerminalFaultObserver : IReceiveEndpointObserver
    {
        readonly object _lock = new();
        readonly List<CancellationTokenSource> _waiting = new();

        Exception? _cause;

        /// <summary>
        /// Gets the terminal failure under the same lock that publishes it before waiter cancellation,
        /// ensuring a released waiter observes the original cause.
        /// </summary>
        public Exception? Cause
        {
            get
            {
                lock (_lock)
                    return _cause;
            }
        }

        /// <summary>Registers a readiness waiter and wakes it immediately if failure has already occurred.</summary>
        /// <param name="waiter">The cancellation source associated with one readiness wait.</param>
        public void Attach(CancellationTokenSource waiter)
        {
            ArgumentNullException.ThrowIfNull(waiter);

            lock (_lock)
            {
                if (_cause == null)
                {
                    _waiting.Add(waiter);
                    return;
                }
            }

            CancelAttachedWaiter(waiter);
        }

        /// <summary>Removes a readiness waiter that no longer needs terminal-fault notification.</summary>
        /// <param name="waiter">The cancellation source associated with one readiness wait.</param>
        public void Detach(CancellationTokenSource waiter)
        {
            ArgumentNullException.ThrowIfNull(waiter);

            lock (_lock)
                _waiting.Remove(waiter);
        }

        static void CancelAttachedWaiter(CancellationTokenSource waiter)
        {
            try
            {
                waiter.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The readiness wait completed while the terminal failure was being published.
            }
            catch (Exception exception)
            {
                // Cancellation callbacks cannot replace the terminal transport failure reported to the waiter.
                LogContext.Warning?.Log(exception, "Bus endpoint readiness cancellation callback faulted");
            }
        }

        static async Task CancelWaitersAsync(CancellationTokenSource[] waiters)
        {
            foreach (var waiter in waiters)
            {
                try
                {
                    await waiter.CancelAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    // The readiness wait completed while the terminal failure was being published.
                }
                catch (Exception exception)
                {
                    // Cancellation callbacks cannot replace the terminal transport failure reported to the waiter.
                    LogContext.Warning?.Log(exception, "Bus endpoint readiness cancellation callback faulted");
                }
            }
        }

        async Task IReceiveEndpointObserver.FaultedAsync(ReceiveEndpointFaulted faulted)
        {
            CancellationTokenSource[] waiting;

            lock (_lock)
            {
                if (_cause != null || !faulted.IsTerminal)
                    return;

                _cause = faulted.Exception;
                waiting = _waiting.ToArray();
                _waiting.Clear();
            }

            await CancelWaitersAsync(waiting).ConfigureAwait(false);
        }

        Task IReceiveEndpointObserver.ReadyAsync(ReceiveEndpointReady ready)
        {
            return Task.CompletedTask;
        }

        Task IReceiveEndpointObserver.StoppingAsync(ReceiveEndpointStopping stopping)
        {
            return Task.CompletedTask;
        }

        Task IReceiveEndpointObserver.CompletedAsync(ReceiveEndpointCompleted completed)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Waits for the bus endpoint and returns the live connection handle.
    /// <para>
    /// If readiness fails, disconnects the handle before propagating the failure so no pipe registration
    /// or request identifier remains attached to the bus.
    /// </para>
    /// </summary>
    /// <param name="handle">The connection to retain only after endpoint readiness succeeds.</param>
    /// <returns>The live connection handle.</returns>
    ConnectHandle WaitForBusEndpoint(ConnectHandle handle)
    {
        try
        {
            WaitUntilBusEndpointIsReady();
        }
        catch
        {
            handle.Disconnect();
            throw;
        }

        return handle;
    }
}
