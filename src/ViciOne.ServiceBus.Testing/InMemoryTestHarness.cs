using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Provides a test harness for in memory test.</summary>
public class InMemoryTestHarness :
    BusTestHarness
{
    readonly InMemoryBusConfiguration _busConfiguration;
    readonly string _inputQueueName;
    readonly IEnumerable<IBusInstanceSpecification> _specifications;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="virtualHost">The virtual host.</param>
    public InMemoryTestHarness(string? virtualHost = null)
        : this(virtualHost, Enumerable.Empty<IBusInstanceSpecification>())
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="virtualHost">The virtual host.</param>
    public InMemoryTestHarness(TimeProvider timeProvider, string? virtualHost = null)
        : this(virtualHost, Enumerable.Empty<IBusInstanceSpecification>(), timeProvider)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="virtualHost">The virtual host.</param>
    /// <param name="specifications">The specifications.</param>
    public InMemoryTestHarness(string? virtualHost, IEnumerable<IBusInstanceSpecification> specifications)
        : this(virtualHost, specifications, TimeProvider.System)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="virtualHost">The virtual host.</param>
    /// <param name="specifications">The specifications.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public InMemoryTestHarness(string? virtualHost, IEnumerable<IBusInstanceSpecification> specifications, TimeProvider timeProvider)
        : base(timeProvider)
    {
        BaseAddress = new Uri("loopback://localhost/");
        if (!string.IsNullOrWhiteSpace(virtualHost))
            BaseAddress = new Uri(BaseAddress, virtualHost.Trim('/') + '/');

        _inputQueueName = "input_queue";
        _busConfiguration = new InMemoryBusConfiguration(new InMemoryTopologyConfiguration(InMemoryBus.CreateMessageTopology()), BaseAddress);
        _specifications = specifications ?? throw new ArgumentNullException(nameof(specifications));

        InputQueueAddress = new Uri(BaseAddress, _inputQueueName);
    }

    /// <summary>Gets the base address.</summary>
    public Uri BaseAddress { get; }

    /// <summary>Gets the input queue address.</summary>
    public override Uri InputQueueAddress { get; }
    /// <summary>Gets the input queue name.</summary>
    public override string InputQueueName => _inputQueueName;

    internal IHostConfiguration HostConfiguration => _busConfiguration.HostConfiguration;

    /// <summary>Occurs when on configure in memory bus.</summary>
    public event Action<IInMemoryBusFactoryConfigurator>? OnConfigureInMemoryBus;
    /// <summary>Occurs when on configure in memory receive endpoint.</summary>
    public event Action<IInMemoryReceiveEndpointConfigurator>? OnConfigureInMemoryReceiveEndpoint;
    /// <summary>Occurs when on in memory bus configured.</summary>
    public event Action<IInMemoryBusFactoryConfigurator>? OnInMemoryBusConfigured;

    /// <summary>Configures in memory bus.</summary>
    /// <param name="configurator">The configurator to update.</param>
    protected virtual void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
    {
        OnConfigureInMemoryBus?.Invoke(configurator);
    }

    /// <summary>Configures in memory receive endpoint.</summary>
    /// <param name="configurator">The configurator to update.</param>
    protected virtual void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
    {
        OnConfigureInMemoryReceiveEndpoint?.Invoke(configurator);
    }

    /// <summary>Reports that in memory bus has been configured.</summary>
    /// <param name="configurator">The configurator to update.</param>
    protected virtual void InMemoryBusConfigured(IInMemoryBusFactoryConfigurator configurator)
    {
        OnInMemoryBusConfigured?.Invoke(configurator);
    }

    /// <summary>Connects request client.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces a handle that disconnects the registration.</returns>
    public virtual Task<IRequestClient<TRequest>> ConnectRequestClientAsync<TRequest>(CancellationToken cancellationToken = default)
        where TRequest : class
    {
        return ConnectRequestClientAsync<TRequest>(InputQueueAddress, cancellationToken: cancellationToken);
    }

    /// <summary>Connects request client.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces a handle that disconnects the registration.</returns>
    public virtual Task<IRequestClient<TRequest>> ConnectRequestClientAsync<TRequest>(Uri destinationAddress, CancellationToken cancellationToken = default)
        where TRequest : class
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IRequestClient<TRequest>>(cancellationToken);

        return Task.FromResult(Bus.CreateRequestClient<TRequest>(destinationAddress, new RequestTimeout(TestTimeout)));
    }

    /// <summary>Creates bus.</summary>
    /// <returns>A task that produces the created value.</returns>
    protected override async Task<IBusControl> CreateBusAsync()
    {
        var configurator = new InMemoryBusFactoryConfigurator(_busConfiguration);

        ConfigureBus(configurator);

        ConfigureInMemoryBus(configurator);

        configurator.ReceiveEndpoint(InputQueueName, e =>
        {
            ConfigureReceiveEndpoint(e);

            ConfigureInMemoryReceiveEndpoint(e);
        });

        BusConfigured(configurator);

        InMemoryBusConfigured(configurator);

        return configurator.Build(_busConfiguration, _specifications);
    }
}
