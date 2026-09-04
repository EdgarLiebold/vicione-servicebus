using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public interface MessageLockContext
{
    Task CompleteAsync(CancellationToken cancellationToken = default);

    Task AbandonAsync(Exception exception, CancellationToken cancellationToken = default);
    Task DeadLetterAsync(CancellationToken cancellationToken = default);
    Task DeadLetterAsync(Exception exception, CancellationToken cancellationToken = default);
}
