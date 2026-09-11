using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Internals.Outgoing;

/// <summary>Applies a send-options snapshot to one typed send context.</summary>
internal sealed class SendOptionsPipe<TMessage>(SendOptions options) :
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
        context.CreateFilterScope("sendOptions");
    }
}
