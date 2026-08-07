// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DependencyInjection
{
    using System;


    public interface IConsumerConsumeScopeContext<out TConsumer, out T> :
        IAsyncDisposable
        where T : class
        where TConsumer : class
    {
        ConsumerConsumeContext<TConsumer, T> Context { get; }
    }
}
