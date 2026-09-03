#nullable enable
namespace ViciOne.ServiceBus.Middleware
{
    using System;
    using System.Threading.Tasks;


    /// <summary>
    /// Places the consumer-local concurrency owner immediately around the real consume pipeline.
    /// </summary>
    internal sealed class ConsumerConcurrencyFilter<TMessage> :
        IFilter<ConsumeContext<TMessage>>
        where TMessage : class
    {
        readonly IConsumerConcurrencyGate<TMessage> _gate;
        readonly ConsumerConcurrencyPolicy _policy;

        public ConsumerConcurrencyFilter(
            IConsumerConcurrencyGate<TMessage> gate,
            ConsumerConcurrencyPolicy policy)
        {
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public Task Send(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);

            return _gate.ExecuteAsync(
                    context.Message,
                    (context, next),
                    static (state, _) => new ValueTask(state.next.Send(state.context)),
                    context.CancellationToken)
                .AsTask();
        }

        public void Probe(ProbeContext context)
        {
            ProbeContext scope = context.CreateFilterScope("consumerConcurrency");
            scope.Add("mode", _policy.Mode.ToString());
            scope.Add("limit", _policy.Concurrency);
        }
    }
}
