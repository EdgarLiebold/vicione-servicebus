using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Provides an in memory bus instance implementation.
/// </summary>
public class InMemoryBusInstance :
    TransportBusInstance<IInMemoryReceiveEndpointConfigurator>,
    IInMemoryDelayProvider
{
    readonly IInMemoryHost _host;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="busControl">The bus control value.</param>
    /// <param name="host">The host value.</param>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="busRegistrationContext">The bus registration context value.</param>
    public InMemoryBusInstance(IBusControl busControl, IHost<IInMemoryReceiveEndpointConfigurator> host, IHostConfiguration hostConfiguration,
        IBusRegistrationContext busRegistrationContext)
        : base(busControl, host, hostConfiguration, busRegistrationContext)
    {
        _host = host as IInMemoryHost ?? throw new ArgumentException("Host was not an IInMemoryHost", nameof(host));
    }

    /// <summary>
    /// Performs the delay operation.
    /// </summary>
    /// <param name="delay">The delay value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        return _host.DelayProvider.DelayAsync(delay, cancellationToken);
    }

    /// <summary>
    /// Performs the delay operation.
    /// </summary>
    /// <param name="delayUntil">The delay until value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task DelayAsync(DateTimeOffset delayUntil, CancellationToken cancellationToken = default)
    {
        return _host.DelayProvider.DelayAsync(delayUntil, cancellationToken);
    }

    /// <summary>
    /// Performs the advance operation.
    /// </summary>
    /// <param name="duration">The duration value.</param>
    public void Advance(TimeSpan duration)
    {
        _host.DelayProvider.Advance(duration);
    }

    /// <summary>
    /// Gets the utc now value.
    /// </summary>
    public DateTimeOffset UtcNow => _host.DelayProvider.UtcNow;
}
