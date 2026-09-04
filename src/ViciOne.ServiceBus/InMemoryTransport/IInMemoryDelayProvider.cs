using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.InMemoryTransport;

public interface IInMemoryDelayProvider
{
    DateTimeOffset UtcNow { get; }

    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
    Task DelayAsync(DateTimeOffset delayUntil, CancellationToken cancellationToken = default);

    void Advance(TimeSpan duration);
}
