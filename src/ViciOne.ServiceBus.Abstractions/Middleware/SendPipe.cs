using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Executes the pipeline for send.</summary>
public class SendPipe :
    ISendPipe
{
    readonly ConcurrentDictionary<Type, IMessagePipe> _outputPipes;
    readonly ISendPipeSpecification _specification;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="specification">The specification.</param>
    public SendPipe(ISendPipeSpecification specification)
    {
        _specification = specification;
        _outputPipes = new ConcurrentDictionary<Type, IMessagePipe>();
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("sendPipe");

        foreach (var outputPipe in _outputPipes.Values)
            outputPipe.Probe(scope);
    }

    /// <summary>Runs the typed send context through its cached message pipeline.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    public Task SendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return _outputPipes.GetOrAdd(typeof(T), x => new MessagePipe<T>(_specification.GetMessageSpecification<T>())).SendAsync(context);
    }


    interface IMessagePipe :
        IProbeSite
    {
        Task SendAsync<T>(SendContext<T> context)
            where T : class;
    }


    class MessagePipe<TMessage> :
        IMessagePipe
        where TMessage : class
    {
        readonly Lazy<IMessageSendPipe<TMessage>> _output;
        readonly IMessageSendPipeSpecification<TMessage> _specification;

        public MessagePipe(IMessageSendPipeSpecification<TMessage> specification)
        {
            _specification = specification;

            _output = new Lazy<IMessageSendPipe<TMessage>>(CreateMessagePipe);
        }

        public Task SendAsync<T>(SendContext<T> context)
            where T : class
        {
            return _output.Value.SendAsync((SendContext<TMessage>)context);
        }

        public void Probe(ProbeContext context)
        {
            _output.Value.Probe(context);
        }

        IMessageSendPipe<TMessage> CreateMessagePipe()
        {
            IPipe<SendContext<TMessage>> messagePipe = _specification.BuildMessagePipe();

            return new MessageSendPipe<TMessage>(messagePipe);
        }
    }
}
