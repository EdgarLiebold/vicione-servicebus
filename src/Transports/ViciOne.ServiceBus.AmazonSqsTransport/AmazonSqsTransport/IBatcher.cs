using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.AmazonSqsTransport;

public interface IBatcher<in TEntry> :
    IAsyncDisposable
{
    Task ExecuteAsync(TEntry entry, CancellationToken cancellationToken = default);
}
