using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Internals.Outgoing;

/// <summary>Applies a publish-options snapshot to one typed publish context.</summary>
internal sealed class PublishOptionsPipe<TMessage>(PublishOptions options) :
    IPipe<PublishContext<TMessage>>
    where TMessage : class
{
    private readonly OutgoingOptionsSnapshot _options = OutgoingOptionsSnapshot.Create(options);

    public Task SendAsync(PublishContext<TMessage> context)
    {
        OutgoingOptionsPipe.Apply(context, _options);
        return Task.CompletedTask;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("publishOptions");
    }
}
