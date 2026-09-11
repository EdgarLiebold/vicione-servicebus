using System.Threading.Tasks;
using ViciOne.ServiceBus.Consumer;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Merges the out-of-band consumer back into the context.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ConsumerMessageMergePipe<TConsumer, TMessage> :
    IPipe<ConsumeContext<TMessage>>
    where TMessage : class
    where TConsumer : class
{
    readonly ConsumerConsumeContext<TConsumer, TMessage> _context;
    readonly IPipe<ConsumerConsumeContext<TConsumer, TMessage>> _output;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="output">The output.</param>
    /// <param name="context">The context associated with the operation.</param>
    public ConsumerMessageMergePipe(IPipe<ConsumerConsumeContext<TConsumer, TMessage>> output, ConsumerConsumeContext<TConsumer, TMessage> context)
    {
        _output = output;
        _context = context;
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
    public Task SendAsync(ConsumeContext<TMessage> context)
    {
        if (ReferenceEquals(context, _context))
            return _output.SendAsync(_context);

        return context is ConsumerConsumeContext<TConsumer, TMessage> consumerContext
            ? _output.SendAsync(consumerContext)
            : _output.SendAsync(new ConsumerConsumeContextScope<TConsumer, TMessage>(context, _context.Consumer));
    }
}
