using System;

namespace ViciOne.ServiceBus;

public interface IDispatchConfigurator<TContext>
{
    void Pipe<T>(Action<IPipeConfigurator<T>> configurePipe)
        where T : class, PipeContext;
}
