using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Records consumer delivery outcomes while delegating consumer creation and invocation.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public class TestConsumerFactoryDecorator<TConsumer> :
    IConsumerFactory<TConsumer>
    where TConsumer : class, IConsumer
{
    readonly IConsumerFactory<TConsumer> _consumerFactory;
    readonly ReceivedMessageList _received;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <param name="received">The received.</param>
    public TestConsumerFactoryDecorator(IConsumerFactory<TConsumer> consumerFactory, ReceivedMessageList received)
    {
        _consumerFactory = consumerFactory;
        _received = received;
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync<TMessage>(ConsumeContext<TMessage> context, IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
        where TMessage : class
    {
        return _consumerFactory.SendAsync(context, new TestDecoratorPipe<TMessage>(_received, next));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateScope("testDecorator");

        _consumerFactory.Probe(scope);
    }


    class TestDecoratorPipe<TMessage> :
        IPipe<ConsumerConsumeContext<TConsumer, TMessage>>
        where TMessage : class
    {
        readonly IPipe<ConsumerConsumeContext<TConsumer, TMessage>> _next;
        readonly ReceivedMessageList _received;

        public TestDecoratorPipe(ReceivedMessageList received, IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
        {
            _received = received;
            _next = next;
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            _next.Probe(context);
        }

        public async Task SendAsync(ConsumerConsumeContext<TConsumer, TMessage> context)
        {
            try
            {
                await _next.SendAsync(context).ConfigureAwait(false);

                _received.Add(context);
            }
            catch (Exception ex)
            {
                _received.Add(context, ex);

                throw;
            }
        }
    }
}
