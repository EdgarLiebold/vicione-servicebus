using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for execute activity registration configurator.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public interface IExecuteActivityRegistrationConfigurator<TActivity, TArguments> :
    IExecuteActivityRegistrationConfigurator
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
}


/// <summary>
/// Defines the contract for execute activity registration configurator.
/// </summary>
public interface IExecuteActivityRegistrationConfigurator
{
    /// <summary>
    /// Performs the endpoint operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    void Endpoint(Action<IEndpointRegistrationConfigurator> configure);
    /// <summary>
    /// Performs the exclude from configure endpoints operation.
    /// </summary>
    void ExcludeFromConfigureEndpoints();
}
