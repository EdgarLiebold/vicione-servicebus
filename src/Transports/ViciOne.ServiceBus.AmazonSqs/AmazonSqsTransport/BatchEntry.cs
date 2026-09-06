using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.AmazonSqs;

sealed class BatchEntry<TEntry> :
    IDisposable
{
    readonly CancellationToken _callerCancellationToken;
    readonly TaskCompletionSource<bool> _completed;
    readonly object _lock = new();
    CancellationTokenRegistration _cancellationRegistration;
    EntryState _state;

    public BatchEntry(TEntry entry, CancellationToken callerCancellationToken)
    {
        Entry = entry;
        _callerCancellationToken = callerCancellationToken;
        _completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (callerCancellationToken.CanBeCanceled)
        {
            CancellationTokenRegistration registration = callerCancellationToken.UnsafeRegister(
                static state => ((BatchEntry<TEntry>)state!).CancelCaller(),
                this);

            var disposeRegistration = false;
            lock (_lock)
            {
                _cancellationRegistration = registration;
                disposeRegistration = _state is EntryState.CanceledBeforeDispatch or EntryState.Terminal;
            }

            if (disposeRegistration)
                registration.Dispose();
        }
    }

    public TEntry Entry { get; }

    public Task Completed => _completed.Task;

    public void Dispose()
    {
        lock (_lock)
            _state = EntryState.Terminal;

        _cancellationRegistration.Dispose();
    }

    public bool TryBeginDispatch()
    {
        lock (_lock)
        {
            if (_state != EntryState.Queued)
                return false;

            _state = EntryState.Dispatched;
            return true;
        }
    }

    public void SetCompleted()
    {
        bool completeCaller;
        lock (_lock)
        {
            completeCaller = _state is EntryState.Queued or EntryState.Dispatched;
            _state = EntryState.Terminal;
        }

        if (completeCaller)
            _completed.TrySetResult(true);

        _cancellationRegistration.Dispose();
    }

    public void SetFaulted(Exception exception)
    {
        bool faultCaller;
        lock (_lock)
        {
            faultCaller = _state is EntryState.Queued or EntryState.Dispatched;
            _state = EntryState.Terminal;
        }

        if (faultCaller)
            _completed.TrySetException(exception);

        _cancellationRegistration.Dispose();
    }

    public void SetCanceled(CancellationToken cancellationToken)
    {
        bool cancelCaller;
        lock (_lock)
        {
            cancelCaller = _state is EntryState.Queued or EntryState.Dispatched;
            _state = EntryState.Terminal;
        }

        if (cancelCaller)
            _completed.TrySetCanceled(cancellationToken);

        _cancellationRegistration.Dispose();
    }

    void CancelCaller()
    {
        lock (_lock)
        {
            if (_state == EntryState.Queued)
                _state = EntryState.CanceledBeforeDispatch;
            else if (_state == EntryState.Dispatched)
                _state = EntryState.CallerCanceledAfterDispatch;
            else
                return;
        }

        _completed.TrySetCanceled(_callerCancellationToken);
    }

    enum EntryState
    {
        Queued,
        Dispatched,
        CanceledBeforeDispatch,
        CallerCanceledAfterDispatch,
        Terminal
    }
}
