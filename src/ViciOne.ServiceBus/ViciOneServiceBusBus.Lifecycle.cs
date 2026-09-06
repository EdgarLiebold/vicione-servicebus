using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus;

internal sealed partial class ViciOneServiceBusBus
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await StartCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    async Task StartCoreAsync(CancellationToken cancellationToken)
    {
        if (_busHandle != null)
        {
            LogContext.Warning?.Log("StartAsync called, but the bus was already started: {Address} ({Reason})", Address, "Already Started");
            return;
        }

        BusLifecycleHandle? busHandle = null;
        CancellationTokenSource? tokenSource = null;
        try
        {
            await _busObservable.PreStartAsync(this).ConfigureAwait(false);

            if (cancellationToken == default)
            {
                tokenSource = new CancellationTokenSource(ReadyTimeout, _timeProvider);
                cancellationToken = tokenSource.Token;
            }

            var hostHandle = _host.Start(cancellationToken);
            busHandle = new BusLifecycleHandle(_host, hostHandle, this, _busObservable, _logContext);

            try
            {
                await busHandle.Ready.OrCanceledAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception) when (exception.CancellationToken == cancellationToken)
            {
                LogContext.Warning?.Log(exception, "Bus start canceled: {HostAddress}", _host.Address);

                try
                {
                    using var stopTimeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30), _timeProvider);
                    await busHandle.StopAsync(stopTimeoutTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception stopException)
                {
                    LogContext.Warning?.Log(stopException, "Bus start canceled, bus stop faulted: {HostAddress}", _host.Address);
                }

                await busHandle.Ready.ConfigureAwait(false);
            }

            await _busObservable.PostStartAsync(this, busHandle.Ready).ConfigureAwait(false);

            _busHandle = busHandle;
            _terminalFault = new TerminalFaultObserver();
            _terminalFaultHandle = (_receiveEndpoint as ReceiveEndpoint)?.ConnectReceiveEndpointObserver(_terminalFault);
            _busState = BusState.Started;
            _healthMessage = "";

            LogContext.Info?.Log("Bus started: {HostAddress}", _host.Address);
        }
        catch (Exception exception)
        {
            try
            {
                if (busHandle != null)
                {
                    LogContext.Warning?.Log(exception, "Bus start faulted: {HostAddress}", _host.Address);

                    using var stopTimeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30), _timeProvider);
                    await busHandle.StopAsync(stopTimeoutTokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // The original startup failure remains authoritative when bounded cleanup is canceled.
            }
            catch (Exception stopException)
            {
                LogContext.Warning?.Log(stopException, "Bus start faulted, bus stop faulted: {HostAddress}", _host.Address);
            }

            _busState = BusState.Faulted;
            _healthMessage = $"start faulted: {exception.Message}";

            try
            {
                await _busObservable.StartFaultedAsync(this, exception).ConfigureAwait(false);
            }
            catch (Exception observerException)
            {
                LogContext.Warning?.Log(observerException,
                    "Bus start-fault observation failed without replacing the startup failure: {HostAddress}", _host.Address);
            }

            throw;
        }
        finally
        {
            tokenSource?.Dispose();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        LogContext.SetCurrentIfNull(_logContext);

        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_busHandle == null)
            {
                LogContext.Warning?.Log("Failed to stop bus: {Address} ({Reason})", Address, "Not Started");
                return;
            }

            await _busHandle.StopAsync(cancellationToken).ConfigureAwait(false);

            _terminalFaultHandle?.Disconnect();
            _terminalFaultHandle = null;
            _terminalFault = null;
            _busHandle = null;
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    sealed class BusLifecycleHandle
    {
        readonly ViciOneServiceBusBus _bus;
        readonly IBusObserver _busObserver;
        readonly IHost _host;
        readonly HostHandle _hostHandle;
        readonly ILogContext _logContext;
        bool _stopped;

        public BusLifecycleHandle(IHost host, HostHandle hostHandle, ViciOneServiceBusBus bus, IBusObserver busObserver, ILogContext logContext)
        {
            _host = host;
            _bus = bus;
            _busObserver = busObserver;
            _logContext = logContext;
            _hostHandle = hostHandle;

            Ready = ReadyOrNotAsync(hostHandle.Ready);
        }

        public Task<BusReady> Ready { get; }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            LogContext.SetCurrentIfNull(_logContext);

            if (_stopped)
                return;

            try
            {
                await _busObserver.PreStopAsync(_bus).ConfigureAwait(false);
                await _hostHandle.StopAsync(cancellationToken).ConfigureAwait(false);
                await _busObserver.PostStopAsync(_bus).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                try
                {
                    await _busObserver.StopFaultedAsync(_bus, exception).ConfigureAwait(false);
                }
                catch (Exception observerException)
                {
                    LogContext.Warning?.Log(observerException,
                        "Bus stop-fault observation failed without replacing the stop failure: {HostAddress}", _host.Address);
                }

                LogContext.Warning?.Log(exception, "Bus stop faulted: {HostAddress}", _host.Address);
                _bus._busState = BusState.Faulted;
                _bus._healthMessage = $"stop faulted: {exception.Message}";
                throw;
            }

            LogContext.Info?.Log("Bus stopped: {HostAddress}", _host.Address);
            _stopped = true;
            _bus._busState = BusState.Stopped;
            _bus._healthMessage = "stopped";
        }

        async Task<BusReady> ReadyOrNotAsync(Task<HostReady> ready)
        {
            var hostReady = await ready.ConfigureAwait(false);
            return new BusReadyEvent(hostReady, _bus);
        }
    }
}
