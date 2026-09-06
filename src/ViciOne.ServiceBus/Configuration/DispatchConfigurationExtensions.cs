using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for dispatch configuration.</summary>
public static class DispatchConfigurationExtensions
{
    /// <summary>
    /// Adds a dispatch filter to the pipe, which can be used to route traffic
    /// based on the type of the incoming context.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="pipeContextProviderFactory">The pipe context provider factory.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void UseDispatch<T>(this IPipeConfigurator<T> configurator, IPipeContextConverterFactory<T> pipeContextProviderFactory,
        Action<IDispatchConfigurator<T>>? configure = null)
        where T : class, PipeContext
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));
        if (pipeContextProviderFactory == null)
            throw new ArgumentNullException(nameof(pipeContextProviderFactory));

        var specification = new DispatchPipeSpecification<T>(pipeContextProviderFactory);

        configure?.Invoke(specification);

        configurator.AddPipeSpecification(specification);
    }
}
