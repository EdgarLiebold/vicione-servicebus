using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures mediator registration.</summary>
public interface IMediatorRegistrationConfigurator :
    IRegistrationConfigurator
{
    /// <summary>Optionally configure the pipeline used by the mediator.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    void ConfigureMediator(Action<IMediatorRegistrationContext, IMediatorConfigurator> configure);
}
