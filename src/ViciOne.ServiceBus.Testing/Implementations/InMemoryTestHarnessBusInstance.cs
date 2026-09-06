using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Represents an instance of in memory test harness bus.</summary>
public class InMemoryTestHarnessBusInstance :
    IBusInstance
{
    readonly IBusRegistrationContext _busRegistrationContext;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="testHarness">The test harness.</param>
    /// <param name="busRegistrationContext">The bus registration context.</param>
    public InMemoryTestHarnessBusInstance(InMemoryTestHarness testHarness, IBusRegistrationContext busRegistrationContext)
    {
        _busRegistrationContext = busRegistrationContext;
        Harness = testHarness;
    }

    /// <summary>Gets the harness.</summary>
    public InMemoryTestHarness Harness { get; }

    /// <summary>Gets the name.</summary>
    public string Name => "vicione-servicebus-bus";
    /// <summary>Gets the instance type.</summary>
    public Type InstanceType => typeof(IBus);
    /// <summary>Gets the bus.</summary>
    public IBus Bus => Harness.Bus;
    /// <summary>Gets the bus control.</summary>
    public IBusControl BusControl => Harness.BusControl;
    /// <summary>Gets the host configuration.</summary>
    public IHostConfiguration HostConfiguration => Harness.HostConfiguration;

    /// <summary>Connects the configured observer or endpoint.</summary>
    /// <typeparam name="TRider">The rider type.</typeparam>
    /// <param name="riderControl">The rider control.</param>
    public void Connect<TRider>(IRiderControl riderControl)
        where TRider : IRider
    {
        throw new NotSupportedException();
    }

    /// <summary>Gets rider.</summary>
    /// <typeparam name="TRider">The rider type.</typeparam>
    /// <returns>The rider.</returns>
    public TRider GetRider<TRider>()
        where TRider : IRider
    {
        throw new NotSupportedException();
    }

    /// <summary>Connects receive endpoint.</summary>
    /// <param name="definition">The definition.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>A handle that disconnects the registration.</returns>
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

    /// <summary>Connects receive endpoint.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>A handle that disconnects the registration.</returns>
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
