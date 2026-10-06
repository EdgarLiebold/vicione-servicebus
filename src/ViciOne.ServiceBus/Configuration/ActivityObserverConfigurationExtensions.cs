using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for activity observer configuration.</summary>
public static class ActivityObserverConfigurationExtensions
{
    /// <summary>Connect an activity observer that will be connected to all activity execute/compensate endpoints.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public static ConnectHandle ConnectActivityObserver(this IBusFactoryConfigurator configurator, IActivityObserver observer)
    {
        return new ActivityConfigurationObserver(configurator, observer);
    }

    /// <summary>Connect an activity observer that will be connected to all activity execute/compensate endpoints.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public static ConnectHandle ConnectActivityObserver(this IReceiveEndpointConfigurator configurator, IActivityObserver observer)
    {
        return new ActivityConfigurationObserver(configurator, observer);
    }


    class ActivityConfigurationObserver :
        IActivityConfigurationObserver,
        ConnectHandle
    {
        readonly List<ConnectHandle> _handles;
        readonly IActivityObserver _observer;
        bool _disposed;

        public ActivityConfigurationObserver(IActivityConfigurationObserverConnector configurator, IActivityObserver observer)
        {
            _observer = observer;
            _handles = new List<ConnectHandle>();

            var handle = configurator.ConnectActivityConfigurationObserver(this);
            _handles.Add(handle);
        }

        public void Dispose() => Retire(dispose: true);

        public void Disconnect() => Retire(dispose: false);

        void Retire(bool dispose)
        {
            if (_disposed)
                return;

            _disposed = true;
            ConnectHandle[] handles = _handles.ToArray();
            _handles.Clear();
            List<Exception>? failures = null;
            foreach (ConnectHandle handle in handles)
            {
                try
                {
                    if (dispose)
                        handle.Dispose();
                    else
                        handle.Disconnect();
                }
                catch (Exception exception)
                {
                    (failures ??= new List<Exception>()).Add(exception);
                }
            }

            if (failures?.Count == 1)
                ExceptionDispatchInfo.Capture(failures[0]).Throw();
            if (failures?.Count > 1)
                throw new AggregateException("One or more activity observer registrations could not be released.", failures);
        }

        public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
            where TActivity : class
            where TArguments : class
        {
            if (_disposed)
                return;

            _handles.Add(configurator.ConnectActivityObserver(_observer));
        }

        public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
            where TActivity : class
            where TArguments : class
        {
            if (_disposed)
                return;

            _handles.Add(configurator.ConnectActivityObserver(_observer));
        }

        public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
            where TActivity : class
            where TLog : class
        {
            if (_disposed)
                return;

            _handles.Add(configurator.ConnectActivityObserver(_observer));
        }
    }
}
