using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Records consumer delivery outcomes while delegating consumer creation and invocation.</summary>
/// <typeparam name="TConsumer">The consumer implementation.</typeparam>
internal sealed class TestConsumerFactoryDecorator<TConsumer> :
    IConsumerFactory<TConsumer>
    where TConsumer : class, IConsumer
{
    readonly IConsumerFactory<TConsumer> _consumerFactory;
    readonly ConsumedMessageList _consumed;

    /// <summary>Creates a decorator over a consumer factory and an observation list.</summary>
    /// <param name="consumerFactory">The consumer factory to invoke.</param>
    /// <param name="consumed">The list that records delivery outcomes.</param>
    public TestConsumerFactoryDecorator(IConsumerFactory<TConsumer> consumerFactory, ConsumedMessageList consumed)
    {
        _consumerFactory = consumerFactory ?? throw new ArgumentNullException(nameof(consumerFactory));
        _consumed = consumed ?? throw new ArgumentNullException(nameof(consumed));
    }

    /// <summary>Invokes the decorated consumer factory and records its consume outcome.</summary>
    /// <typeparam name="TMessage">The message contract consumed by the factory.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync<TMessage>(ConsumeContext<TMessage> context, IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return _consumerFactory.SendAsync(context, new TestDecoratorPipe<TMessage>(_consumed, next));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateScope("testDecorator");

        _consumerFactory.Probe(scope);
    }


    sealed class TestDecoratorPipe<TMessage> :
        IPipe<ConsumerConsumeContext<TConsumer, TMessage>>
        where TMessage : class
    {
        readonly IPipe<ConsumerConsumeContext<TConsumer, TMessage>> _next;
        readonly ConsumedMessageList _consumed;

        public TestDecoratorPipe(ConsumedMessageList consumed, IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
        {
            _consumed = consumed ?? throw new ArgumentNullException(nameof(consumed));
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            _next.Probe(context);
        }

        public async Task SendAsync(ConsumerConsumeContext<TConsumer, TMessage> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            try
            {
                await _next.SendAsync(context).ConfigureAwait(false);

                _consumed.Add(context);
            }
            catch (Exception ex)
            {
                _consumed.Add(context, ex);

                throw;
            }
        }
    }
}
