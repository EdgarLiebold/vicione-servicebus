using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Hosts an isolated in-memory bus and records its message activity for tests.</summary>
public class InMemoryTestHarness :
    BusTestHarness
{
    readonly InMemoryBusConfiguration _busConfiguration;
    readonly string _inputQueueName;
    readonly IEnumerable<IBusInstanceSpecification> _specifications;

    /// <summary>Creates a harness using the system time provider and no additional bus specifications.</summary>
    /// <param name="virtualHost">An optional path segment that isolates the in-memory transport address.</param>
    public InMemoryTestHarness(string? virtualHost = null)
        : this(virtualHost, Enumerable.Empty<IBusInstanceSpecification>())
    {
    }

    /// <summary>Creates a harness using the specified time provider and no additional bus specifications.</summary>
    /// <param name="timeProvider">The time source used by test timeouts and inactivity detection.</param>
    /// <param name="virtualHost">An optional path segment that isolates the in-memory transport address.</param>
    public InMemoryTestHarness(TimeProvider timeProvider, string? virtualHost = null)
        : this(virtualHost, Enumerable.Empty<IBusInstanceSpecification>(), timeProvider)
    {
    }

    /// <summary>Creates a harness using the specified bus-instance specifications.</summary>
    /// <param name="virtualHost">An optional path segment that isolates the in-memory transport address.</param>
    /// <param name="specifications">The specifications applied when the bus is built.</param>
    public InMemoryTestHarness(string? virtualHost, IEnumerable<IBusInstanceSpecification> specifications)
        : this(virtualHost, specifications, TimeProvider.System)
    {
    }

    /// <summary>Creates a harness using the specified bus-instance specifications and time provider.</summary>
    /// <param name="virtualHost">An optional path segment that isolates the in-memory transport address.</param>
    /// <param name="specifications">The specifications applied when the bus is built.</param>
    /// <param name="timeProvider">The time source used by test timeouts and inactivity detection.</param>
    public InMemoryTestHarness(string? virtualHost, IEnumerable<IBusInstanceSpecification> specifications, TimeProvider timeProvider)
        : base(timeProvider)
    {
        ArgumentNullException.ThrowIfNull(specifications);
        if (virtualHost != null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(virtualHost);

            virtualHost = virtualHost.Trim('/');
            ArgumentException.ThrowIfNullOrWhiteSpace(virtualHost);
        }

        BaseAddress = new Uri("loopback://localhost/");
        if (virtualHost != null)
            BaseAddress = new Uri(BaseAddress, virtualHost + '/');

        _inputQueueName = "input_queue";
        _busConfiguration = new InMemoryBusConfiguration(new InMemoryTopologyConfiguration(InMemoryBus.CreateMessageTopology()), BaseAddress);
        _specifications = specifications;

        InputQueueAddress = new Uri(BaseAddress, _inputQueueName);
    }

    /// <summary>Gets the root transport address used by the in-memory bus.</summary>
    public Uri BaseAddress { get; }

    /// <summary>Gets the transport address of the harness receive queue.</summary>
    public override Uri InputQueueAddress { get; }
    /// <summary>Gets the name of the harness receive queue.</summary>
    public override string InputQueueName => _inputQueueName;

    /// <summary>Occurs while the harness configures the in-memory bus factory.</summary>
    public event Action<IInMemoryBusFactoryConfigurator>? InMemoryBusConfiguring;
    /// <summary>Occurs while the harness configures its in-memory receive endpoint.</summary>
    public event Action<IInMemoryReceiveEndpointConfigurator>? InMemoryReceiveEndpointConfiguring;
    /// <summary>Occurs after the in-memory bus factory has been fully configured.</summary>
    public event Action<IInMemoryBusFactoryConfigurator>? InMemoryBusConfigured;

    /// <summary>Applies subscribers' configuration to the in-memory bus.</summary>
    /// <param name="configurator">The in-memory bus configurator.</param>
    protected virtual void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
    {
        InMemoryBusConfiguring?.Invoke(configurator);
    }

    /// <summary>Applies subscribers' configuration to the in-memory receive endpoint.</summary>
    /// <param name="configurator">The in-memory receive-endpoint configurator.</param>
    protected virtual void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
    {
        InMemoryReceiveEndpointConfiguring?.Invoke(configurator);
    }

    /// <summary>Notifies subscribers after in-memory bus configuration has completed.</summary>
    /// <param name="configurator">The completed in-memory bus configurator.</param>
    protected virtual void NotifyInMemoryBusConfigured(IInMemoryBusFactoryConfigurator configurator)
    {
        InMemoryBusConfigured?.Invoke(configurator);
    }

    /// <summary>Builds the configured in-memory bus.</summary>
    /// <param name="cancellationToken">Cancellation checked before synchronous bus construction begins.</param>
    /// <returns>A task whose result is the configured bus control.</returns>
    protected override Task<IBusControl> CreateBusAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IBusControl>(cancellationToken);

        var configurator = new InMemoryBusFactoryConfigurator(_busConfiguration);

        ConfigureBus(configurator);

        ConfigureInMemoryBus(configurator);

        configurator.ReceiveEndpoint(InputQueueName, e =>
        {
            ConfigureReceiveEndpoint(e);

            ConfigureInMemoryReceiveEndpoint(e);
        });

        NotifyBusConfigured(configurator);

        NotifyInMemoryBusConfigured(configurator);

        return Task.FromResult(configurator.Build(_busConfiguration, _specifications));
    }
}
