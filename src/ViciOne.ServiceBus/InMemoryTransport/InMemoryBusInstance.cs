using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport;

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

    public Task Delay(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        return _host.DelayProvider.Delay(delay, cancellationToken);
    }

    public Task Delay(DateTimeOffset delayUntil, CancellationToken cancellationToken = default)
    {
        return _host.DelayProvider.Delay(delayUntil, cancellationToken);
    }

    public void Advance(TimeSpan duration)
    {
        _host.DelayProvider.Advance(duration);
    }

    public DateTimeOffset UtcNow => _host.DelayProvider.UtcNow;
}
