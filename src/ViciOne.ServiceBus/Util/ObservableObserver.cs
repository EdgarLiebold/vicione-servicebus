using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Util;

/// <summary>Observes observable events.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ObservableObserver<T> :
    IObservable<T>,
    IObserver<T>
{
    readonly Connectable<IObserver<T>> _observers;

    /// <summary>Initializes a new instance.</summary>
    public ObservableObserver()
    {
        _observers = new Connectable<IObserver<T>>();
    }

    /// <summary>Subscribes to the configured event source.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>The disposable produced by the operation.</returns>
    public IDisposable Subscribe(IObserver<T> observer)
    {
        return _observers.Connect(observer);
    }

    /// <summary>Handles the notification for next.</summary>
    /// <param name="value">The value to process.</param>
    public void OnNext(T value)
    {
        _observers.ForEachAsync(x =>
        {
            x.OnNext(value);

            return Task.CompletedTask;
        });
    }

    /// <summary>Handles the notification for error.</summary>
    /// <param name="error">The error.</param>
    public void OnError(Exception error)
    {
        _observers.ForEachAsync(x =>
        {
            x.OnError(error);

            return Task.CompletedTask;
        });
    }

    /// <summary>Reports that on has completed.</summary>
    public void OnCompleted()
    {
        _observers.ForEachAsync(x =>
        {
            x.OnCompleted();

            return Task.CompletedTask;
        });
    }
}
