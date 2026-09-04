using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for dispatch configurator.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public interface IDispatchConfigurator<TContext>
{
    /// <summary>
    /// Performs the pipe operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurePipe">The configure pipe value.</param>
    void Pipe<T>(Action<IPipeConfigurator<T>> configurePipe)
        where T : class, PipeContext;
}
