// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DependencyInjection
{
    using System;


    public interface ICompensateScopeContext<out TLog> :
        IAsyncDisposable
        where TLog : class
    {
        CompensateContext<TLog> Context { get; }

        T GetService<T>()
            where T : class;
    }
}
