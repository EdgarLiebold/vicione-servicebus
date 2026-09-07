using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Creates the default dependency-injection bus instance from a registration callback.</summary>
public class RegistrationBusFactory :
    IRegistrationBusFactory
{
    readonly Func<IBusRegistrationContext, IBusControl> _configure;

    /// <summary>Creates a factory that obtains a bus control from a registration context.</summary>
    /// <param name="configure">The callback that creates the bus control.</param>
    public RegistrationBusFactory(Func<IBusRegistrationContext, IBusControl> configure)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    /// <summary>Creates the default bus instance for a registration context.</summary>
    /// <param name="context">The context that supplies registered services and endpoint configuration.</param>
    /// <param name="specifications">The bus-instance specifications supplied by the registration pipeline.</param>
    /// <param name="busName">The registered bus name.</param>
    /// <returns>The created bus instance.</returns>
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

        public IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter endpointNameFormatter,
            Action<IBusRegistrationContext, IReceiveEndpointConfigurator>? configure = null)
        {
            return BusControl.ConnectReceiveEndpoint(definition, endpointNameFormatter, configurator =>
            {
                _busRegistrationContext.GetConfigureReceiveEndpoints()
                    .Configure(definition.GetEndpointName(endpointNameFormatter), configurator);

                configure?.Invoke(_busRegistrationContext, configurator);
            });
        }

        public IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName,
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
