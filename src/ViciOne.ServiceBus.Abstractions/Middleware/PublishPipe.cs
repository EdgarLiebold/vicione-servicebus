using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a publish pipe implementation.
/// </summary>
public class PublishPipe :
    IPublishPipe
{
    readonly ConcurrentDictionary<Type, IMessagePipe> _outputPipes;
    readonly IPublishPipeSpecification _specification;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public PublishPipe(IPublishPipeSpecification specification)
    {
        _specification = specification;
        _outputPipes = new ConcurrentDictionary<Type, IMessagePipe>();
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("publishPipe");

        foreach (var outputPipe in _outputPipes.Values)
            outputPipe.Probe(scope);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    public Task SendAsync<T>(PublishContext<T> context, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return _outputPipes.GetOrAdd(typeof(T), x => new MessagePipe<T>(_specification.GetMessageSpecification<T>())).SendAsync(context);
    }


    interface IMessagePipe :
        IProbeSite
    {
        Task SendAsync<T>(PublishContext<T> context)
            where T : class;
    }


    class MessagePipe<TMessage> :
        IMessagePipe
        where TMessage : class
    {
        readonly Lazy<IMessagePublishPipe<TMessage>> _output;
        readonly IMessagePublishPipeSpecification<TMessage> _specification;

        public MessagePipe(IMessagePublishPipeSpecification<TMessage> specification)
        {
            _output = new Lazy<IMessagePublishPipe<TMessage>>(CreateMessagePipe);

            _specification = specification;
        }

        public Task SendAsync<T>(PublishContext<T> context)
            where T : class
        {
            return _output.Value.SendAsync((PublishContext<TMessage>)context);
        }

        public void Probe(ProbeContext context)
        {
            _output.Value.Probe(context);
        }

        IMessagePublishPipe<TMessage> CreateMessagePipe()
        {
            IPipe<PublishContext<TMessage>> messagePipe = _specification.BuildMessagePipe();

            return new MessagePublishPipe<TMessage>(messagePipe);
        }
    }
}
