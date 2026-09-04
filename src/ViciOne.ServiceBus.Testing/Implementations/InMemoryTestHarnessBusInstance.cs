using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Provides an in memory test harness bus instance implementation.
/// </summary>
public class InMemoryTestHarnessBusInstance :
    IBusInstance
{
    readonly IBusRegistrationContext _busRegistrationContext;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="testHarness">The test harness value.</param>
    /// <param name="busRegistrationContext">The bus registration context value.</param>
    public InMemoryTestHarnessBusInstance(InMemoryTestHarness testHarness, IBusRegistrationContext busRegistrationContext)
    {
        _busRegistrationContext = busRegistrationContext;
        Harness = testHarness;
    }

    /// <summary>
    /// Gets the harness value.
    /// </summary>
    public InMemoryTestHarness Harness { get; }

    /// <summary>
    /// Gets the name value.
    /// </summary>
    public string Name => "vicione-servicebus-bus";
    /// <summary>
    /// Gets the instance type value.
    /// </summary>
    public Type InstanceType => typeof(IBus);
    /// <summary>
    /// Gets the bus value.
    /// </summary>
    public IBus Bus => Harness.Bus;
    /// <summary>
    /// Gets the bus control value.
    /// </summary>
    public IBusControl BusControl => Harness.BusControl;
    /// <summary>
    /// Gets the host configuration value.
    /// </summary>
    public IHostConfiguration HostConfiguration => Harness.HostConfiguration;

    /// <summary>
    /// Performs the connect operation.
    /// </summary>
    /// <typeparam name="TRider">The t rider type.</typeparam>
    /// <param name="riderControl">The rider control value.</param>
    public void Connect<TRider>(IRiderControl riderControl)
        where TRider : IRider
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Gets rider.
    /// </summary>
    /// <typeparam name="TRider">The t rider type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public TRider GetRider<TRider>()
        where TRider : IRider
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Connects receive endpoint.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Connects receive endpoint.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
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
