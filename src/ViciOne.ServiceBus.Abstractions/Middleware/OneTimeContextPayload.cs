using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

class OneTimeContextPayload<TPayload> :
    OneTimeContext<TPayload>
    where TPayload : class
{
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
        }
    }

    public Task RunOneTime(Func<OneTimeSetupMethod> oneTimeSetupMethodFactory)
    {
        ArgumentNullException.ThrowIfNull(oneTimeSetupMethodFactory);

        lock (this)
        {
            if (HasValue || _running)
                return _value.Task;

            if (IsFaultedOrCanceled)
                _value = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            OneTimeSetupMethod setup = oneTimeSetupMethodFactory()
                ?? throw new InvalidOperationException("The one-time setup method factory returned null.");

            _running = true;
            _ = RunSetup(setup, _value);
            return _value.Task;
        }
    }

    async Task RunSetup(OneTimeSetupMethod setup, TaskCompletionSource<bool> completion)
    {
        try
        {
            await setup.SetupPayload().ConfigureAwait(false);
            completion.TrySetResult(true);
        }
        catch (OperationCanceledException canceled)
        {
            completion.TrySetCanceled(canceled.CancellationToken);
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
        finally
        {
            lock (this)
            {
                if (ReferenceEquals(_value, completion))
                    _running = false;
            }
        }
    }
}
