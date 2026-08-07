// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

using System;
using System.Threading.Tasks;
using Contracts.JobService;


public interface INotifyJobContext
{
    Task NotifyCanceled();
    Task NotifyStarted();
    Task NotifyCompleted();
    Task NotifyFaulted(Exception exception, TimeSpan? delay = default);
    Task NotifyJobProgress(SetJobProgress progress);
}
