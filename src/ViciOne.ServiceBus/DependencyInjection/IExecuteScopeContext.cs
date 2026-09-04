using System;

namespace ViciOne.ServiceBus.DependencyInjection;

public interface IExecuteScopeContext<out TArguments> :
    IAsyncDisposable
    where TArguments : class
{
    ExecuteContext<TArguments> Context { get; }

    T GetService<T>()
        where T : class;
}
