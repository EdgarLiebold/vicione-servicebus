using System;

namespace ViciOne.ServiceBus.DependencyInjection;

public interface ICompensateScopeContext<out TLog> :
    IAsyncDisposable
    where TLog : class
{
    CompensateContext<TLog> Context { get; }

    T GetService<T>()
        where T : class;
}
