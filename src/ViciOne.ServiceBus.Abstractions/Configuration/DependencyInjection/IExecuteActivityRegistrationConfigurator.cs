using System;

namespace ViciOne.ServiceBus;

public interface IExecuteActivityRegistrationConfigurator<TActivity, TArguments> :
    IExecuteActivityRegistrationConfigurator
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
}


public interface IExecuteActivityRegistrationConfigurator
{
    void Endpoint(Action<IEndpointRegistrationConfigurator> configure);
    void ExcludeFromConfigureEndpoints();
}
