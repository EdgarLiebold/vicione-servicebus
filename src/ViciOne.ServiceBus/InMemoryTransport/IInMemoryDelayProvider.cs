using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.InMemoryTransport;

public interface IInMemoryDelayProvider
{
    DateTimeOffset UtcNow { get; }

    Task Delay(TimeSpan delay, CancellationToken cancellationToken = default);
    Task Delay(DateTimeOffset delayUntil, CancellationToken cancellationToken = default);

    void Advance(TimeSpan duration);
}
