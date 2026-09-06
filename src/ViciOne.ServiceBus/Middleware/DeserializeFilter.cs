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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="serializers">The serializers.</param>
    /// <param name="output">The output.</param>
    public DeserializeFilter(ISerialization serializers, IPipe<ConsumeContext> output)
    {
        _serializers = serializers;
        _output = output;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("deserialize");

        _serializers.Probe(scope);
        _output.Probe(scope);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [DebuggerNonUserCode]
    public async Task SendAsync(ReceiveContext context, IPipe<ReceiveContext> next)
    {
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
