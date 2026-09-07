using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Used to reschedule delivery of the current message.</summary>
public interface MessageRedeliveryContext
{
    /// <summary>Schedules the current message for redelivery after a delay.</summary>
    /// <param name="delay">The minimum delay before the message becomes eligible for redelivery.</param>
    /// <param name="callback">An optional callback that configures the outgoing redelivery context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ScheduleRedeliveryAsync(TimeSpan delay, Action<ConsumeContext, SendContext>? callback = null, CancellationToken cancellationToken = default);
}
