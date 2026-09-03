namespace ViciOne.ServiceBus
{
    using System;


    public interface IActivityRegistrationConfigurator<TActivity, TArguments, TLog> :
        IActivityRegistrationConfigurator
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
    }


    public interface IActivityRegistrationConfigurator
    {
        /// <summary>
        /// Configure the activity's execute endpoint
        /// </summary>
        /// <param name="configureExecute"></param>
        IActivityRegistrationConfigurator ExecuteEndpoint(Action<IEndpointRegistrationConfigurator> configureExecute);

        /// <summary>
        /// Configure the activity's compensate endpoint
        /// </summary>
        /// <param name="configureCompensate"></param>
        IActivityRegistrationConfigurator CompensateEndpoint(Action<IEndpointRegistrationConfigurator> configureCompensate);

        void ExcludeFromConfigureEndpoints();
    }
}
