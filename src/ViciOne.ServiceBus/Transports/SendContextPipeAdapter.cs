using System.Threading.Tasks;

#nullable enable
namespace ViciOne.ServiceBus.Transports;

public abstract class SendContextPipeAdapter<TMessage> :
    IPipe<SendContext<TMessage>>,
    ISendPipe
    where TMessage : class
{
    readonly IPipe<SendContext<TMessage>>? _pipe;

    protected SendContextPipeAdapter(IPipe<SendContext<TMessage>>? pipe)
    {
        _pipe = pipe;
    }

    public void Probe(ProbeContext context)
    {
        _pipe?.Probe(context);
    }

    Task IPipe<SendContext<TMessage>>.SendAsync(SendContext<TMessage> context)
    {
        Send(context);

        return _pipe.IsNotEmpty() ? _pipe!.SendAsync(context) : Task.CompletedTask;
    }

    Task ISendContextPipe.SendAsync<T>(SendContext<T> context, CancellationToken cancellationToken)
        where T : class
    {
        Send(context);

        return _pipe is ISendContextPipe sendContextPipe
            ? sendContextPipe.SendAsync(context, cancellationToken: cancellationToken)
            : Task.CompletedTask;
    }

    protected abstract void Send(SendContext<TMessage> context);

    protected abstract void Send<T>(SendContext<T> context)
        where T : class;
}
