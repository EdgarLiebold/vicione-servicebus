using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds synchronous or awaited delegate callbacks to a middleware pipeline.</summary>
public static class DelegateConfigurationExtensions
{
    /// <summary>Adds a synchronous callback to the pipeline.</summary>
    /// <typeparam name="TContext">The pipeline context.</typeparam>
    /// <param name="configurator">The pipeline to configure.</param>
    /// <param name="callback">The callback invoked for each context.</param>
    public static void UseExecute<TContext>(this IPipeConfigurator<TContext> configurator, Action<TContext> callback)
        where TContext : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(callback);

        var pipeBuilderConfigurator = new DelegatePipeSpecification<TContext>(callback);

        configurator.AddPipeSpecification(pipeBuilderConfigurator);
    }

    /// <summary>Adds a callback whose returned task is awaited by the pipeline.</summary>
    /// <typeparam name="TContext">The pipeline context.</typeparam>
    /// <param name="configurator">The pipeline to configure.</param>
    /// <param name="callback">The awaited callback invoked for each context.</param>
    public static void UseExecuteAwaited<TContext>(this IPipeConfigurator<TContext> configurator, Func<TContext, Task> callback)
        where TContext : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(callback);

        var pipeBuilderConfigurator = new AsyncDelegatePipeSpecification<TContext>(callback);

        configurator.AddPipeSpecification(pipeBuilderConfigurator);
    }
}
