using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers mediator handlers and configures its in-process pipelines.</summary>
public interface IMediatorRegistrationConfigurator :
    IRegistrationConfigurator
{
    /// <summary>Configures the mediator pipeline after registered handlers are available.</summary>
    /// <param name="configure">The callback that receives the registration context and mediator configurator.</param>
    void ConfigureMediator(Action<IMediatorRegistrationContext, IMediatorConfigurator> configure);
}
