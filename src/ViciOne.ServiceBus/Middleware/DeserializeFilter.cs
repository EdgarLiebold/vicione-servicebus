using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Performs the deserialization of a message ReceiveContext and passes the resulting
/// ConsumeContext to the output pipe.
/// </summary>
public class DeserializeFilter :
    IFilter<ReceiveContext>
{
    readonly IPipe<ConsumeContext> _output;
    readonly ISerialization _serializers;

    public DeserializeFilter(ISerialization serializers, IPipe<ConsumeContext> output)
    {
        _serializers = serializers;
        _output = output;
    }

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("deserialize");

        _serializers.Probe(scope);
        _output.Probe(scope);
    }

    [DebuggerNonUserCode]
    public async Task SendAsync(ReceiveContext context, IPipe<ReceiveContext> next)
    {
        if (!context.TryGetPayload(out ConsumeContext? consumeContext))
            consumeContext = _serializers.GetMessageDeserializer(context.ContentType).Deserialize(context);

        Activity.Current?.AddConsumeContextTags(consumeContext);

        await _output.SendAsync(consumeContext).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);

        await consumeContext.ConsumeCompleted.ConfigureAwait(false);
    }
}
