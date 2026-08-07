// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DependencyInjection
{
    using System;


    public interface IExecuteScopeContext<out TArguments> :
        IAsyncDisposable
        where TArguments : class
    {
        ExecuteContext<TArguments> Context { get; }

        T GetService<T>()
            where T : class;
    }
}
