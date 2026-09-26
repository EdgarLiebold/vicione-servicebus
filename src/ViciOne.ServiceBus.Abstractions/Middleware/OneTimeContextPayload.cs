using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

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

    public Task RunOneTimeAsync(Func<OneTimeSetupMethod> oneTimeSetupMethodFactory)
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
            TaskCompletionSource<bool> completion = _value;
            _ = RunSetupAsync(setup, completion);
            return completion.Task;
        }
    }

    async Task RunSetupAsync(OneTimeSetupMethod setup, TaskCompletionSource<bool> completion)
    {
        Exception? failure = null;
        try
        {
            await setup.SetupPayloadAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        lock (this)
        {
            if (ReferenceEquals(_value, completion))
                _running = false;

            if (failure is OperationCanceledException canceled)
                completion.TrySetCanceled(canceled.CancellationToken);
            else if (failure != null)
                completion.TrySetException(failure);
            else
                completion.TrySetResult(true);
        }
    }
}
