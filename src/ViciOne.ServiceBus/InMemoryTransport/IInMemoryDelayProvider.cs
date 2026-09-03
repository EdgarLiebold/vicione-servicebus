namespace ViciOne.ServiceBus.InMemoryTransport;

using System;
using System.Threading;
using System.Threading.Tasks;


public interface IInMemoryDelayProvider
{
    DateTimeOffset UtcNow { get; }

    Task Delay(TimeSpan delay, CancellationToken cancellationToken = default);
    Task Delay(DateTimeOffset delayUntil, CancellationToken cancellationToken = default);

    void Advance(TimeSpan duration);
}
