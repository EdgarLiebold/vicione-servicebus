namespace ViciOne.ServiceBus.Configuration
{
    using System;
    using Util;


    public class ActivityConfigurationObservable :
        Connectable<IActivityConfigurationObserver>,
        IActivityConfigurationObserver
    {
        public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
            where TActivity : class, IExecuteActivity<TArguments>
            where TArguments : class
        {
            ArgumentNullException.ThrowIfNull(configurator);
            ArgumentNullException.ThrowIfNull(compensateAddress);

            ForEach(observer => observer.ActivityConfigured(configurator, compensateAddress));
        }

        public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator)
            where TActivity : class, IExecuteActivity<TArguments>
            where TArguments : class
        {
            ArgumentNullException.ThrowIfNull(configurator);

            ForEach(observer => observer.ExecuteActivityConfigured(configurator));
        }

        public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityConfigurator<TActivity, TLog> configurator)
            where TActivity : class, ICompensateActivity<TLog>
            where TLog : class
        {
            ArgumentNullException.ThrowIfNull(configurator);

            ForEach(observer => observer.CompensateActivityConfigured(configurator));
        }
    }
}
