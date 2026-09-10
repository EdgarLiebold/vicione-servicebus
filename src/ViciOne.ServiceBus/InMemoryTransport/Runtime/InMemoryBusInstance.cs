using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport.Runtime;

/// <summary>Adapts a registered in-memory bus to the bus-instance and logical-clock contracts.</summary>
internal sealed class InMemoryBusInstance :
    TransportBusInstance<IInMemoryReceiveEndpointConfigurator>,
    IInMemoryDelayProvider
{
    readonly IInMemoryHost _host;

    /// <summary>Creates a registered bus instance over an in-memory host.</summary>
    /// <param name="busControl">The bus control owned by the instance.</param>
    /// <param name="host">The in-memory host.</param>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="busRegistrationContext">The dependency-injection registration context.</param>
    public InMemoryBusInstance(IBusControl busControl, IHost<IInMemoryReceiveEndpointConfigurator> host, IHostConfiguration hostConfiguration,
        IBusRegistrationContext busRegistrationContext)
        : base(busControl, host, hostConfiguration, busRegistrationContext)
    {
        _host = host as IInMemoryHost
            ?? throw new ArgumentException("The host does not expose the in-memory host contract.", nameof(host));
    }

    /// <summary>Returns a task that completes after a relative logical-time delay.</summary>
    /// <param name="delay">The non-negative delay duration.</param>
    /// <param name="cancellationToken">The token that cancels the pending delay.</param>
    /// <returns>A task that completes when the deadline is reached.</returns>
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        return _host.DelayProvider.DelayAsync(delay, cancellationToken);
    }

    /// <summary>Returns a task that completes at an absolute logical-time deadline.</summary>
    /// <param name="delayUntil">The UTC deadline.</param>
    /// <param name="cancellationToken">The token that cancels the pending delay.</param>
    /// <returns>A task that completes when the deadline is reached.</returns>
    public Task DelayAsync(DateTimeOffset delayUntil, CancellationToken cancellationToken = default)
    {
        return _host.DelayProvider.DelayAsync(delayUntil, cancellationToken);
    }

    /// <summary>Advances logical time and releases every due delay.</summary>
    /// <param name="duration">The positive duration by which logical time advances.</param>
    public void Advance(TimeSpan duration)
    {
        _host.DelayProvider.Advance(duration);
    }

    /// <summary>Gets the current logical UTC time.</summary>
    public DateTimeOffset UtcNow => _host.DelayProvider.UtcNow;
}
