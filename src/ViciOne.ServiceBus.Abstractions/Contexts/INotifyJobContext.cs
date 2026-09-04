using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus;

public interface INotifyJobContext
{
    Task NotifyCanceled();
    Task NotifyStarted();
    Task NotifyCompleted();
    Task NotifyFaulted(Exception exception, TimeSpan? delay = default);
    Task NotifyJobProgress(SetJobProgress progress);
}
