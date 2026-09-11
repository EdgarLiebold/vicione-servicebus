using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Applies adapter-specific metadata before forwarding a typed or untyped send pipe.</summary>
/// <typeparam name="TMessage">The message contract handled by the typed pipe.</typeparam>
public abstract class SendContextPipeAdapter<TMessage> :
    IPipe<SendContext<TMessage>>,
    ISendPipe
    where TMessage : class
{
    readonly IPipe<SendContext<TMessage>>? _pipe;

    /// <summary>Initializes an adapter around an optional caller-supplied typed send pipe.</summary>
    /// <param name="pipe">The typed send pipe invoked after adapter metadata has been applied.</param>
    protected SendContextPipeAdapter(IPipe<SendContext<TMessage>>? pipe)
    {
        _pipe = pipe;
    }

    /// <summary>Adds diagnostics from the wrapped send pipe to a probe.</summary>
    /// <param name="context">The probe context that receives the diagnostics.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _pipe?.Probe(context);
    }

    Task IPipe<SendContext<TMessage>>.SendAsync(SendContext<TMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Send(context);

        IPipe<SendContext<TMessage>>? pipe = _pipe;
        return pipe != null && pipe.IsNotEmpty()
            ? pipe.SendAsync(context) ?? throw new InvalidOperationException("The wrapped typed send pipe returned no task.")
            : Task.CompletedTask;
    }

    Task ISendContextPipe.SendAsync<T>(SendContext<T> context, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        Send(context);

        return _pipe is ISendContextPipe sendContextPipe
            ? sendContextPipe.SendAsync(context, cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("The wrapped send-context pipe returned no task.")
            : Task.CompletedTask;
    }

    /// <summary>Applies adapter metadata to the declared message contract.</summary>
    /// <param name="context">The typed send context being configured.</param>
    protected abstract void Send(SendContext<TMessage> context);

    /// <summary>Applies adapter metadata through the general send-context contract.</summary>
    /// <typeparam name="T">The runtime message contract.</typeparam>
    /// <param name="context">The send context being configured.</param>
    protected abstract void Send<T>(SendContext<T> context)
        where T : class;
}
