using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Adapts an in-memory test harness to the bus-instance registration contract.</summary>
public class InMemoryTestHarnessBusInstance :
    IBusInstance
{
    readonly IBusRegistrationContext _busRegistrationContext;

    /// <summary>Creates a bus instance backed by an in-memory test harness.</summary>
    /// <param name="testHarness">The harness that owns the bus and its host configuration.</param>
    /// <param name="busRegistrationContext">The context used to configure dynamically connected endpoints.</param>
    public InMemoryTestHarnessBusInstance(InMemoryTestHarness testHarness, IBusRegistrationContext busRegistrationContext)
    {
        _busRegistrationContext = busRegistrationContext;
        Harness = testHarness;
    }

    /// <summary>Gets the in-memory test harness backing this instance.</summary>
    public InMemoryTestHarness Harness { get; }

    /// <summary>Gets the stable default bus registration name.</summary>
    public string Name => "vicione-servicebus-bus";
    /// <summary>Gets the bus contract type represented by this instance.</summary>
    public Type InstanceType => typeof(IBus);
    /// <summary>Gets the running harness bus.</summary>
    public IBus Bus => Harness.Bus;
    /// <summary>Gets the harness bus lifecycle control.</summary>
    public IBusControl BusControl => Harness.BusControl;
    /// <summary>Gets the harness host configuration.</summary>
    public IHostConfiguration HostConfiguration => Harness.HostConfiguration;

    /// <summary>Rejects rider registration because this harness instance exposes only its in-memory bus.</summary>
    /// <typeparam name="TRider">The requested rider contract.</typeparam>
    /// <param name="riderControl">The rider lifecycle controller.</param>
    public void Connect<TRider>(IRiderControl riderControl)
        where TRider : IRider
    {
        throw new NotSupportedException();
    }

    /// <summary>Rejects rider resolution because this harness instance exposes no riders.</summary>
    /// <typeparam name="TRider">The requested rider contract.</typeparam>
    /// <returns>This method does not return.</returns>
    public TRider GetRider<TRider>()
        where TRider : IRider
    {
        throw new NotSupportedException();
    }

    /// <summary>Connects an endpoint described by a definition and applies registered endpoint configuration.</summary>
    /// <param name="definition">The definition that supplies endpoint identity and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the endpoint name.</param>
    /// <param name="configure">An optional callback that configures the endpoint through registered services.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
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

    /// <summary>Connects a named endpoint and applies registered endpoint configuration.</summary>
    /// <param name="queueName">The in-memory queue name.</param>
    /// <param name="configure">An optional callback that configures the endpoint through registered services.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
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
