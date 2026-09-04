using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for mediator registration configurator.
/// </summary>
public interface IMediatorRegistrationConfigurator :
    IRegistrationConfigurator
{
    /// <summary>
    /// Optionally configure the pipeline used by the mediator
    /// </summary>
    /// <param name="configure"></param>
    void ConfigureMediator(Action<IMediatorRegistrationContext, IMediatorConfigurator> configure);
}
