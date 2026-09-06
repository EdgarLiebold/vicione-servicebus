using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>Represents an instance of in memory bus.</summary>
public class InMemoryBusInstance :
    TransportBusInstance<IInMemoryReceiveEndpointConfigurator>,
    IInMemoryDelayProvider
{
    readonly IInMemoryHost _host;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="busControl">The bus control.</param>
    /// <param name="host">The host.</param>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="busRegistrationContext">The bus registration context.</param>
    public InMemoryBusInstance(IBusControl busControl, IHost<IInMemoryReceiveEndpointConfigurator> host, IHostConfiguration hostConfiguration,
        IBusRegistrationContext busRegistrationContext)
        : base(busControl, host, hostConfiguration, busRegistrationContext)
    {
        _host = host as IInMemoryHost ?? throw new ArgumentException("Host was not an IInMemoryHost", nameof(host));
    }

    /// <summary>Delays the operation for the configured duration.</summary>
    /// <param name="delay">The delay before the operation is attempted.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        return _host.DelayProvider.DelayAsync(delay, cancellationToken);
    }

    /// <summary>Delays the operation for the configured duration.</summary>
    /// <param name="delayUntil">The delay until.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DelayAsync(DateTimeOffset delayUntil, CancellationToken cancellationToken = default)
    {
        return _host.DelayProvider.DelayAsync(delayUntil, cancellationToken);
    }

    /// <summary>Advances the current state.</summary>
    /// <param name="duration">The duration.</param>
    public void Advance(TimeSpan duration)
    {
        _host.DelayProvider.Advance(duration);
    }

    /// <summary>Gets the utc now.</summary>
    public DateTimeOffset UtcNow => _host.DelayProvider.UtcNow;
}
