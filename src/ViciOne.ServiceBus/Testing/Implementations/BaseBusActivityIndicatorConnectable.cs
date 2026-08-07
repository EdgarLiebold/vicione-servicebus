// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Testing.Implementations
{
    using System.Threading.Tasks;
    using Util;


    public abstract class BaseBusActivityIndicatorConnectable : Connectable<IConditionObserver>,
        IObservableCondition
    {
        public ConnectHandle ConnectConditionObserver(IConditionObserver observer)
        {
            return Connect(observer);
        }

        public abstract bool IsMet { get; }

        protected Task ConditionUpdated()
        {
            return ForEachAsync(x => x.ConditionUpdated());
        }
    }
}
