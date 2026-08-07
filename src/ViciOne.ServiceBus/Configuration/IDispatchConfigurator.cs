// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public interface IDispatchConfigurator<TContext>
    {
        void Pipe<T>(Action<IPipeConfigurator<T>> configurePipe)
            where T : class, PipeContext;
    }
}
