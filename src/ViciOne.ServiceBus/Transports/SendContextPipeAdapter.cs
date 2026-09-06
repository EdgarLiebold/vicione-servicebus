using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Adapts send context pipe between component contracts.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public abstract class SendContextPipeAdapter<TMessage> :
    IPipe<SendContext<TMessage>>,
    ISendPipe
    where TMessage : class
{
    readonly IPipe<SendContext<TMessage>>? _pipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="pipe">The pipeline stages to apply.</param>
    protected SendContextPipeAdapter(IPipe<SendContext<TMessage>>? pipe)
    {
        _pipe = pipe;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
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

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    protected abstract void Send(SendContext<TMessage> context);

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    protected abstract void Send<T>(SendContext<T> context)
        where T : class;
}
