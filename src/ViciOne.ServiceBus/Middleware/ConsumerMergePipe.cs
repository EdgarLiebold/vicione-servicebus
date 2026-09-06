using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Merges the out-of-band consumer back into the pipe.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ConsumerMergePipe<TConsumer, TMessage> :
    IPipe<ConsumerConsumeContext<TConsumer>>
    where TMessage : class
    where TConsumer : class
{
    readonly IPipe<ConsumerConsumeContext<TConsumer, TMessage>> _output;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="output">The output.</param>
    public ConsumerMergePipe(IPipe<ConsumerConsumeContext<TConsumer, TMessage>> output)
    {
        _output = output;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("merge");
        scope.Set(new
        {
            ConsumerType = TypeCache<TConsumer>.ShortName,
            MessageType = TypeCache<TMessage>.ShortName
        });

        _output.Probe(scope);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(ConsumerConsumeContext<TConsumer> context)
    {
        if (context is ConsumerConsumeContext<TConsumer, TMessage> consumerContext)
            return _output.SendAsync(consumerContext);

        if (context.TryGetMessage(out ConsumeContext<TMessage>? messageContext))
            return _output.SendAsync(new ConsumerConsumeContextScope<TConsumer, TMessage>(messageContext, context.Consumer));

        throw new ArgumentException($"The message could not be retrieved: {TypeCache<TMessage>.ShortName}", nameof(context));
    }
}
