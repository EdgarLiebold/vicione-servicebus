using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// Provides an observable observer implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ObservableObserver<T> :
    IObservable<T>,
    IObserver<T>
{
    readonly Connectable<IObserver<T>> _observers;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ObservableObserver()
    {
        _observers = new Connectable<IObserver<T>>();
    }

    /// <summary>
    /// Performs the subscribe operation.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public IDisposable Subscribe(IObserver<T> observer)
    {
        return _observers.Connect(observer);
    }

    /// <summary>
    /// Performs the on next operation.
    /// </summary>
    /// <param name="value">The value.</param>
    public void OnNext(T value)
    {
        _observers.ForEachAsync(x =>
        {
            x.OnNext(value);

            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Performs the on error operation.
    /// </summary>
    /// <param name="error">The error value.</param>
    public void OnError(Exception error)
    {
        _observers.ForEachAsync(x =>
        {
            x.OnError(error);

            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Performs the on completed operation.
    /// </summary>
    public void OnCompleted()
    {
        _observers.ForEachAsync(x =>
        {
            x.OnCompleted();

            return Task.CompletedTask;
        });
    }
}
