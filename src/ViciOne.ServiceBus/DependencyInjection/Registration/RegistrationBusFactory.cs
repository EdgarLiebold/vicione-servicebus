using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Creates registration bus instances.</summary>
public class RegistrationBusFactory :
    IRegistrationBusFactory
{
    readonly Func<IBusRegistrationContext, IBusControl> _configure;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public RegistrationBusFactory(Func<IBusRegistrationContext, IBusControl> configure)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    /// <summary>Creates bus.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="specifications">The specifications.</param>
    /// <param name="busName">The bus name.</param>
    /// <returns>The created bus.</returns>
    public IBusInstance CreateBus(IBusRegistrationContext context, IEnumerable<IBusInstanceSpecification> specifications, string busName)
    {
        LogContext.ConfigureCurrentLogContextIfNull(context);

        var busControl = _configure(context);

        return new DefaultBusInstance(busControl, context);
    }


    class DefaultBusInstance :
        IBusInstance
    {
        const string RiderExceptionMessage =
            "Riders could be only used with Microsoft DI or Autofac using 'SetBusFactory' method (UsingTransport extensions).";

        readonly IBusRegistrationContext _busRegistrationContext;

        public DefaultBusInstance(IBusControl busControl, IBusRegistrationContext busRegistrationContext)
        {
            _busRegistrationContext = busRegistrationContext;
            BusControl = busControl;
        }

        public string Name => "vicione-servicebus-bus";
        public Type InstanceType => typeof(IBus);
        public IBus Bus => BusControl;
        public IBusControl BusControl { get; }

        public IHostConfiguration HostConfiguration => throw new ConfigurationException(
            global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Default Bus Instance", "unknown", "Host configuration is unavailable for the default registration bus instance.", "Correct the named configuration before starting the host"));

        public void Connect<TRider>(IRiderControl riderControl)
            where TRider : IRider
        {
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Default Bus Instance", "unknown", RiderExceptionMessage, "Correct the named configuration before starting the host"));
        }

        public TRider GetRider<TRider>()
            where TRider : IRider
        {
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Default Bus Instance", "unknown", RiderExceptionMessage, "Correct the named configuration before starting the host"));
        }

        public HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter endpointNameFormatter,
            Action<IBusRegistrationContext, IReceiveEndpointConfigurator>? configure = null)
        {
            return BusControl.ConnectReceiveEndpoint(definition, endpointNameFormatter, configurator =>
            {
                _busRegistrationContext.GetConfigureReceiveEndpoints()
                    .Configure(definition.GetEndpointName(endpointNameFormatter), configurator);

                configure?.Invoke(_busRegistrationContext, configurator);
            });
        }

        public HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName,
            Action<IBusRegistrationContext, IReceiveEndpointConfigurator>? configure = null)
        {
            return BusControl.ConnectReceiveEndpoint(queueName, configurator =>
            {
                _busRegistrationContext.GetConfigureReceiveEndpoints().Configure(queueName, configurator);

                configure?.Invoke(_busRegistrationContext, configurator);
            });
        }
    }
}
