// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.InMemoryTransport;

using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using Transports;


public class InMemoryBusInstance :
    TransportBusInstance<IInMemoryReceiveEndpointConfigurator>,
    IInMemoryDelayProvider
{
    readonly IInMemoryHost _host;

    public InMemoryBusInstance(IBusControl busControl, IHost<IInMemoryReceiveEndpointConfigurator> host, IHostConfiguration hostConfiguration,
        IBusRegistrationContext busRegistrationContext)
        : base(busControl, host, hostConfiguration, busRegistrationContext)
    {
        _host = host as IInMemoryHost ?? throw new ArgumentException("Host was not an IInMemoryHost", nameof(host));
    }

    public Task Delay(int milliseconds, CancellationToken cancellationToken = default)
    {
        return _host.DelayProvider.Delay(milliseconds, cancellationToken);
    }

    public Task Delay(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        return _host.DelayProvider.Delay(delay, cancellationToken);
    }

    public Task Delay(DateTime delayUntil, CancellationToken cancellationToken = default)
    {
        return _host.DelayProvider.Delay(delayUntil, cancellationToken);
    }

    public ValueTask Advance(TimeSpan duration)
    {
        return _host.DelayProvider.Advance(duration);
    }
}
