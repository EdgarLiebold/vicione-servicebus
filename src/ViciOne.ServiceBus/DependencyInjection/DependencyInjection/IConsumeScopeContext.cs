// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DependencyInjection
{
    using System;


    public interface IConsumeScopeContext :
        IAsyncDisposable
    {
        ConsumeContext Context { get; }
    }


    public interface IConsumeScopeContext<out TMessage> :
        IAsyncDisposable
        where TMessage : class
    {
        ConsumeContext<TMessage> Context { get; }

        T GetService<T>()
            where T : class;

        T CreateInstance<T>(params object[] arguments)
            where T : class;

        IDisposable PushConsumeContext(ConsumeContext context);
    }
}
