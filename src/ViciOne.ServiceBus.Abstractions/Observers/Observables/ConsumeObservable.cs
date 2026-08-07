// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Observables
{
    using System;
    using System.Threading.Tasks;
    using Util;


    public class ConsumeObservable :
        Connectable<IConsumeObserver>,
        IConsumeObserver
    {
        public Task PreConsume<T>(ConsumeContext<T> context)
            where T : class
        {
            return ForEachAsync(x => x.PreConsume(context));
        }

        public Task PostConsume<T>(ConsumeContext<T> context)
            where T : class
        {
            return ForEachAsync(x => x.PostConsume(context));
        }

        public Task ConsumeFault<T>(ConsumeContext<T> context, Exception exception)
            where T : class
        {
            return ForEachAsync(x => x.ConsumeFault(context, exception));
        }
    }
}
