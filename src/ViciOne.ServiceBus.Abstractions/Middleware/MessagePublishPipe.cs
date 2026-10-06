using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Forwards a typed publish context to the retained output pipeline.</summary>
/// <typeparam name="TMessage">The message contract carried by the publish context.</typeparam>
public class MessagePublishPipe<TMessage> :
    IMessagePublishPipe<TMessage>
    where TMessage : class
{
    readonly IPipe<PublishContext<TMessage>> _outputPipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="outputPipe">The output pipe.</param>
    public MessagePublishPipe(IPipe<PublishContext<TMessage>> outputPipe)
    {
        _outputPipe = outputPipe;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("messagePublishPipe");
        scope.Add("messageType", TypeCache<TMessage>.ShortName);

        _outputPipe.Probe(scope);
    }

    [DebuggerNonUserCode]
    Task IPipe<PublishContext<TMessage>>.SendAsync(PublishContext<TMessage> context)
    {
        return _outputPipe.SendAsync(context);
    }
}
