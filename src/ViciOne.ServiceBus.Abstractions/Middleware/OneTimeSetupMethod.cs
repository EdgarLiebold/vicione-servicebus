namespace ViciOne.ServiceBus;

using System;
using System.Threading.Tasks;
using Internals;


class OneTimeSetupMethod
{
    readonly OneTimeSetupCallback _callback;
    readonly TaskCompletionSource<bool> _value;

    public OneTimeSetupMethod(OneTimeSetupCallback callback)
    {
        _callback = callback;
        _value = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public Task<bool> Value => _value.Task;

    public Task SetupPayload()
    {
        try
        {
            Task task = _callback()
                ?? throw new InvalidOperationException("The one-time setup callback returned null.");
            if (task.Status == TaskStatus.RanToCompletion)
            {
                _value.TrySetResult(true);

                return Task.CompletedTask;
            }

            async Task SetupAsync()
            {
                try
                {
                    await task.ConfigureAwait(false);

                    _value.TrySetResult(true);
                }
                catch (Exception exception)
                {
                    SetFailure(exception);

                    throw;
                }
            }

            return SetupAsync();
        }
        catch (Exception exception)
        {
            SetFailure(exception);

            throw;
        }
    }

    public void SetPayload(Task<bool> value)
    {
        _value.TrySetFromTask(value);
    }

    void SetFailure(Exception exception)
    {
        if (exception is OperationCanceledException canceled)
            _value.TrySetCanceled(canceled.CancellationToken);
        else
            _value.TrySetException(exception);
    }
}


public interface OneTimeContext
{
    void Evict();
}
