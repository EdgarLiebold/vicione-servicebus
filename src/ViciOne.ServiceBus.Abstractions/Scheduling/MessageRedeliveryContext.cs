using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>
/// Used to reschedule delivery of the current message
/// </summary>
public interface MessageRedeliveryContext
{
    /// <summary>
    /// Schedule the message to be redelivered after the specified delay with given operation.
    /// </summary>
    /// <param name="delay">The minimum delay before the message will be redelivered to the queue</param>
    /// <param name="callback">Operation which perform during message redeliver to queue</param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task ScheduleRedeliveryAsync(TimeSpan delay, Action<ConsumeContext, SendContext>? callback = null, CancellationToken cancellationToken = default);
}
