using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Merges the out-of-band message back into the pipe
/// </summary>
/// <typeparam name="TSaga"></typeparam>
/// <typeparam name="TMessage"></typeparam>
public class SagaMergePipe<TSaga, TMessage> :
    IPipe<SagaConsumeContext<TSaga>>
    where TMessage : class
    where TSaga : class, ISaga
{
    readonly IPipe<SagaConsumeContext<TSaga, TMessage>> _output;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="output">The output value.</param>
    public SagaMergePipe(IPipe<SagaConsumeContext<TSaga, TMessage>> output)
    {
        _output = output;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("merge");
        scope.Set(new
        {
            SagaType = TypeCache<TSaga>.ShortName,
            MessageType = TypeCache<TMessage>.ShortName
        });

        _output.Probe(scope);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(SagaConsumeContext<TSaga> context)
    {
        if (context is SagaConsumeContext<TSaga, TMessage> consumerContext)
            return _output.SendAsync(consumerContext);

        throw new ArgumentException($"The message could not be retrieved: {TypeCache<TMessage>.ShortName}", nameof(context));
    }
}
