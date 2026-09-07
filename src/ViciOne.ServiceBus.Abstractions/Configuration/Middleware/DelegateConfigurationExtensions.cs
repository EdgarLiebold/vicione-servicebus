using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for delegate configuration.</summary>
public static class DelegateConfigurationExtensions
{
    /// <summary>Executes a synchronous method on the pipe.</summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="configurator">The pipe configurator.</param>
    /// <param name="callback">The callback to invoke.</param>
    public static void UseExecute<TContext>(this IPipeConfigurator<TContext> configurator, Action<TContext> callback)
        where TContext : class, PipeContext
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var pipeBuilderConfigurator = new DelegatePipeSpecification<TContext>(callback);

        configurator.AddPipeSpecification(pipeBuilderConfigurator);
    }

    /// <summary>Executes an asynchronous method on the pipe.</summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="configurator">The pipe configurator.</param>
    /// <param name="callback">The callback to invoke.</param>
    public static void UseExecuteAwaited<TContext>(this IPipeConfigurator<TContext> configurator, Func<TContext, Task> callback)
        where TContext : class, PipeContext
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var pipeBuilderConfigurator = new AsyncDelegatePipeSpecification<TContext>(callback);

        configurator.AddPipeSpecification(pipeBuilderConfigurator);
    }
}
