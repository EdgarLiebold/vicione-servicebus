namespace ViciOne.ServiceBus.Tests.InternalAccess.Transports;

public sealed class TerminalFaultObserverTestDriver
{
    private readonly ViciOneServiceBusBus.TerminalFaultObserver _observer = new();

    public Exception? Cause => _observer.Cause;

    public void Attach(CancellationTokenSource waiter) => _observer.Attach(waiter);

    public Task FaultedAsync(ReceiveEndpointFaulted faulted) =>
        ((IReceiveEndpointObserver)_observer).FaultedAsync(faulted);
}
