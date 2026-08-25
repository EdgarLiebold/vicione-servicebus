namespace ViciOne.ServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;


class OneTimeContextPayload<TPayload> :
    OneTimeContext<TPayload>
    where TPayload : class
{
    Queue<OneTimeSetupMethod>? _pending;
    bool _running;
    TaskCompletionSource<bool> _value;

    public OneTimeContextPayload()
    {
        _value = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public Task Value => _value.Task;

    public bool HasValue => _value.Task.Status == TaskStatus.RanToCompletion;

    public bool IsFaultedOrCanceled => _value.Task.IsFaulted || _value.Task.IsCanceled;

    public void Evict()
    {
        lock (this)
        {
            if (_running)
                throw new InvalidOperationException("A one-time setup cannot be evicted while it is running.");

            _value = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending = null;
        }
    }

    public Task RunOneTime(Func<OneTimeSetupMethod> oneTimeSetupMethodFactory)
    {
        lock (this)
        {
            if (HasValue)
                return _value.Task;

            if (IsFaultedOrCanceled && !_running)
                _value = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            OneTimeSetupMethod pendingValue = oneTimeSetupMethodFactory()
                ?? throw new InvalidOperationException("The one-time setup method factory returned null.");

            if (!_running)
            {
                _running = true;
                _ = RunSetupSequence(pendingValue);
            }
            else
                (_pending ??= new Queue<OneTimeSetupMethod>(1)).Enqueue(pendingValue);

            return pendingValue.Value;
        }
    }

    async Task RunSetupSequence(OneTimeSetupMethod oneTimeSetupMethod)
    {
        while (true)
        {
            try
            {
                await oneTimeSetupMethod.SetupPayload().ConfigureAwait(false);

                SetResult(true);
                return;
            }
            catch (Exception ex)
            {
                if (!TryTakeOrCompleteFailure(ex, out OneTimeSetupMethod? pendingValue))
                    return;

                oneTimeSetupMethod = pendingValue;
            }
        }
    }

    bool TryTakeOrCompleteFailure(Exception exception, [NotNullWhen(true)] out OneTimeSetupMethod? pendingValue)
    {
        lock (this)
        {
            if (_pending is { Count: > 0 })
            {
                pendingValue = _pending.Dequeue();
                return true;
            }

            if (exception is OperationCanceledException canceled)
                _value.TrySetCanceled(canceled.CancellationToken);
            else
                _value.TrySetException(exception);
            _running = false;
            _pending = null;
        }

        pendingValue = default;
        return false;
    }

    void SetResult(bool value)
    {
        lock (this)
        {
            _value.TrySetResult(value);

            while (_pending is { Count: > 0 })
                _pending.Dequeue().SetPayload(_value.Task);

            _running = false;
            _pending = null;
        }
    }
}
