using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for latest configuration.</summary>
public static class LatestConfigurationExtensions
{
    /// <summary>Adds a latest value filter to the pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void UseLatest<T>(this IPipeConfigurator<T> configurator, Action<ILatestConfigurator<T>>? configure = null)
        where T : class, PipeContext
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var pipeBuilderConfigurator = new LatestPipeSpecification<T>();

        configure?.Invoke(pipeBuilderConfigurator);

        configurator.AddPipeSpecification(pipeBuilderConfigurator);
    }
}
