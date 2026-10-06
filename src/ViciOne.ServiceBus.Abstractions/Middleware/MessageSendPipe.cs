using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Forwards a typed send context to the retained output pipeline.</summary>
/// <typeparam name="TOutput">The message contract carried by the send context.</typeparam>
public class MessageSendPipe<TOutput> :
    IMessageSendPipe<TOutput>
    where TOutput : class
{
    readonly IPipe<SendContext<TOutput>> _outputPipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="outputPipe">The output pipe.</param>
    public MessageSendPipe(IPipe<SendContext<TOutput>> outputPipe)
    {
        _outputPipe = outputPipe;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("messageSendPipe");
        scope.Add("outputType", TypeCache<TOutput>.ShortName);

        _outputPipe.Probe(scope);
    }

    [DebuggerNonUserCode]
    Task IPipe<SendContext<TOutput>>.SendAsync(SendContext<TOutput> context)
    {
        return _outputPipe.SendAsync(context);
    }
}
