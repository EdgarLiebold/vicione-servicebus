// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Util
{
    using System;
    using System.Threading.Tasks;


    public class ObservableObserver<T> :
        IObservable<T>,
        IObserver<T>
    {
        readonly Connectable<IObserver<T>> _observers;

        public ObservableObserver()
        {
            _observers = new Connectable<IObserver<T>>();
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return _observers.Connect(observer);
        }

        public void OnNext(T value)
        {
            _observers.ForEachAsync(x =>
            {
                x.OnNext(value);

                return Task.CompletedTask;
            });
        }

        public void OnError(Exception error)
        {
            _observers.ForEachAsync(x =>
            {
                x.OnError(error);

                return Task.CompletedTask;
            });
        }

        public void OnCompleted()
        {
            _observers.ForEachAsync(x =>
            {
                x.OnCompleted();

                return Task.CompletedTask;
            });
        }
    }
}
