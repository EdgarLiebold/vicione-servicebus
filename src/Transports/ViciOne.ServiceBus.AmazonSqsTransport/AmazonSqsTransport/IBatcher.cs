// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AmazonSqsTransport;

using System;
using System.Threading;
using System.Threading.Tasks;


public interface IBatcher<in TEntry> :
    IAsyncDisposable
{
    Task Execute(TEntry entry, CancellationToken cancellationToken = default);
}
