using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Internals.Outgoing;

/// <summary>Applies a schedule-options snapshot to one typed send context.</summary>
internal sealed class ScheduleOptionsPipe<TMessage>(ScheduleOptions options) :
    IPipe<SendContext<TMessage>>
    where TMessage : class
{
    private readonly OutgoingOptionsSnapshot _options = OutgoingOptionsSnapshot.Create(options);

    public Task SendAsync(SendContext<TMessage> context)
    {
        OutgoingOptionsPipe.Apply(context, _options);
        return Task.CompletedTask;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("scheduleOptions");
    }
}
