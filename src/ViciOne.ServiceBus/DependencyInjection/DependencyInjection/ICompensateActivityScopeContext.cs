// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DependencyInjection
{
    using System;


    public interface ICompensateActivityScopeContext<out TActivity, out TLog> :
        IAsyncDisposable
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        CompensateActivityContext<TActivity, TLog> Context { get; }

        T GetService<T>()
            where T : class;
    }
}
