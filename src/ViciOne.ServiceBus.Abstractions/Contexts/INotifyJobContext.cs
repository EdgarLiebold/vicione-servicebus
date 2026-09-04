using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus;

public interface INotifyJobContext
{
    Task NotifyCanceledAsync(CancellationToken cancellationToken = default);
    Task NotifyStartedAsync(CancellationToken cancellationToken = default);
    Task NotifyCompletedAsync(CancellationToken cancellationToken = default);
    Task NotifyFaultedAsync(Exception exception, TimeSpan? delay = default, CancellationToken cancellationToken = default);
    Task NotifyJobProgressAsync(SetJobProgress progress, CancellationToken cancellationToken = default);
}
