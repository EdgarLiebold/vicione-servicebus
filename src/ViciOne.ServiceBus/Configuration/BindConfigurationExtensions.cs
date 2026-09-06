using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for bind configuration.</summary>
public static class BindConfigurationExtensions
{
    /// <summary>Adds a filter to the pipe which is of a different type than the native pipe context type.</summary>
    /// <typeparam name="TLeft">The context type.</typeparam>
    /// <param name="configurator">The pipe configurator.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void UseBind<TLeft>(this IPipeConfigurator<TLeft> configurator, Action<IBindConfigurator<TLeft>> configure)
        where TLeft : class, PipeContext
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var bindConfigurator = new BindConfigurator<TLeft>(configurator);

        configure?.Invoke(bindConfigurator);
    }
}
