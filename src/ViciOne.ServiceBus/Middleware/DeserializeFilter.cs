using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging.Diagnostics;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Deserializes a received message, enriches its active trace, and dispatches the resulting consume context.
/// </summary>
public class DeserializeFilter :
    IFilter<ReceiveContext>
{
    readonly IPipe<ConsumeContext> _output;
    readonly ISerialization _serializers;

    /// <summary>Initializes the filter with its serializer registry and consume pipeline.</summary>
    /// <param name="serializers">The serializer registry selected by content type.</param>
    /// <param name="output">The consume pipeline that receives the deserialized context.</param>
    public DeserializeFilter(ISerialization serializers, IPipe<ConsumeContext> output)
    {
        _serializers = serializers ?? throw new ArgumentNullException(nameof(serializers));
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("deserialize");

        _serializers.Probe(scope);
        _output.Probe(scope);
    }

    /// <summary>Validates, deserializes, and dispatches one received message before continuing the receive pipeline.</summary>
    /// <param name="context">The transport receive context.</param>
    /// <param name="next">The remaining receive pipeline.</param>
    /// <returns>A task that completes after consume and receive processing finish.</returns>
    [DebuggerNonUserCode]
    public async Task SendAsync(ReceiveContext context, IPipe<ReceiveContext> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context.TryGetPayload(out MessageLimits? limits)
            && context.Body.Length is { } actualBytes
            && actualBytes > limits.MaxEnvelopeBytes)
        {
            throw new MessageTooLargeException(actualBytes, limits.MaxEnvelopeBytes, context.InputAddress);
        }

        if (!context.TryGetPayload(out ConsumeContext? consumeContext))
            consumeContext = _serializers.GetMessageDeserializer(context.ContentType).Deserialize(context);

        Activity.Current?.AddConsumeContextTags(consumeContext);

        await _output.SendAsync(consumeContext).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);

        await consumeContext.ConsumeCompleted.ConfigureAwait(false);
    }
}
