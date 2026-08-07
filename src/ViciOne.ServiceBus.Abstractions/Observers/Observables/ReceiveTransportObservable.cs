// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Observables
{
    using System.Threading.Tasks;
    using Util;


    public class ReceiveTransportObservable :
        Connectable<IReceiveTransportObserver>,
        IReceiveTransportObserver
    {
        public Task Ready(ReceiveTransportReady ready)
        {
            return ForEachAsync(x => x.Ready(ready));
        }

        public Task Completed(ReceiveTransportCompleted completed)
        {
            return ForEachAsync(x => x.Completed(completed));
        }

        public Task Faulted(ReceiveTransportFaulted faulted)
        {
            return ForEachAsync(x => x.Faulted(faulted));
        }
    }
}
