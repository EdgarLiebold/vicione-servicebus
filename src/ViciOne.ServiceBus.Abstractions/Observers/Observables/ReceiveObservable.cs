// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Observables
{
    using System;
    using System.Threading.Tasks;
    using Util;


    public class ReceiveObservable :
        Connectable<IReceiveObserver>,
        IReceiveObserver
    {
        public Task PreReceive(ReceiveContext context)
        {
            return ForEachAsync(x => x.PreReceive(context));
        }

        public Task PostReceive(ReceiveContext context)
        {
            return ForEachAsync(x => x.PostReceive(context));
        }

        public Task PostConsume<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class
        {
            return ForEachAsync(x => x.PostConsume(context, duration, consumerType));
        }

        public Task ConsumeFault<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
            where T : class
        {
            return ForEachAsync(x => x.ConsumeFault(context, duration, consumerType, exception));
        }

        public Task ReceiveFault(ReceiveContext context, Exception exception)
        {
            return ForEachAsync(x => x.ReceiveFault(context, exception));
        }
    }
}
