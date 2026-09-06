using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures dispatch.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IDispatchConfigurator<TContext>
{
    /// <summary>Builds the configured pipeline.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurePipe">The configure pipe.</param>
    void Pipe<T>(Action<IPipeConfigurator<T>> configurePipe)
        where T : class, PipeContext;
}
