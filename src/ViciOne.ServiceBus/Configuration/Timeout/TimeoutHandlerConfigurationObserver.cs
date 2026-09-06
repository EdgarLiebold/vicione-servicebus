using System;

namespace ViciOne.ServiceBus.Configuration;

internal sealed class TimeoutHandlerConfigurationObserver :
    IHandlerConfigurationObserver
{
    readonly Action<ITimeoutConfigurator> _configure;

    public TimeoutHandlerConfigurationObserver(Action<ITimeoutConfigurator> configure)
    {
        _configure = configure;
    }

    void IHandlerConfigurationObserver.HandlerConfigured<T>(IHandlerConfigurator<T> configurator)
    {
        var specification = new TimeoutSpecification<T>();

        _configure(specification);

        configurator.AddPipeSpecification(specification);
    }
}
