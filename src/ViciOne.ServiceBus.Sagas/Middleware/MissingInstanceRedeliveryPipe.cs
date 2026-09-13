using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Middleware;

internal sealed class MissingInstanceRedeliveryPipe<TSaga, TMessage> :
    IPipe<ConsumeContext<TMessage>>
    where TSaga : SagaStateMachineInstance
    where TMessage : class
{
    readonly IPipe<ConsumeContext<TMessage>> _finalPipe;
    readonly RetryObservable _observers;
    readonly RedeliveryOptions _options;
    readonly IRetryPolicy _retryPolicy;

    public MissingInstanceRedeliveryPipe(
        IRetryPolicy retryPolicy,
        RetryObservable observers,
        IPipe<ConsumeContext<TMessage>> finalPipe,
        RedeliveryOptions options)
    {
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        _observers = observers ?? throw new ArgumentNullException(nameof(observers));
        _finalPipe = finalPipe ?? throw new ArgumentNullException(nameof(finalPipe));
        _options = options;
    }

    public async Task SendAsync(ConsumeContext<TMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CancellationToken.ThrowIfCancellationRequested();

        RetryPolicyContext<ConsumeContext<TMessage>> policyContext = _retryPolicy.CreatePolicyContext(context)
            ?? throw new InvalidOperationException("The retry policy returned a null policy context.");
        using (policyContext)
        {
            if (policyContext.Context == null)
                throw new InvalidOperationException("The retry policy returned a policy context without a consume context.");

            if (_observers.Count > 0)
                await _observers.PostCreateAsync(policyContext).ConfigureAwait(false);

            var exception = new SagaException("An existing saga instance was not found", typeof(TSaga), typeof(TMessage), context.CorrelationId ?? Guid.Empty);

            if (!policyContext.CanRetry(exception, out RetryContext<ConsumeContext<TMessage>> retryContext))
            {
                await NotifyRetryFaultAsync(context, retryContext, exception).ConfigureAwait(false);
                await _finalPipe.SendAsync(context).ConfigureAwait(false);
                return;
            }

            var previousDeliveryCount = context.Advanced().GetRedeliveryCount();
            for (var retryIndex = 0; retryIndex < previousDeliveryCount; retryIndex++)
            {
                if (!retryContext.CanRetry(exception, out retryContext))
                {
                    await NotifyRetryFaultAsync(context, retryContext, exception).ConfigureAwait(false);
                    await _finalPipe.SendAsync(context).ConfigureAwait(false);
                    return;
                }
            }

            if (_observers.Count > 0)
                await _observers.PostFaultAsync(retryContext).ConfigureAwait(false);

            var redeliveryContext = _options.HasFlag(RedeliveryOptions.ConfigureMessageScheduler)
                ? (MessageRedeliveryContext)new ScheduleMessageRedeliveryContext<TMessage>(context, _options)
                : new DelayedMessageRedeliveryContext<TMessage>(context, _options);

            var delay = retryContext.Delay ?? TimeSpan.Zero;

            await redeliveryContext.ScheduleRedeliveryAsync(delay).ConfigureAwait(false);
        }
    }

    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateScope("missingInstanceRedelivery");
        scope.Add("sagaType", TypeCache<TSaga>.ShortName);
        scope.Add("messageType", TypeCache<TMessage>.ShortName);
        scope.Add("replaceMessageId", _options.HasFlag(RedeliveryOptions.ReplaceMessageId));
        scope.Add("configureMessageScheduler", _options.HasFlag(RedeliveryOptions.ConfigureMessageScheduler));
        _retryPolicy.Probe(scope);
    }

    async Task NotifyRetryFaultAsync(
        ConsumeContext<TMessage> context,
        RetryContext<ConsumeContext<TMessage>> retryContext,
        Exception exception)
    {
        if (!_retryPolicy.IsHandled(exception))
            return;

        context.GetOrAddPayload(() => retryContext);
        await retryContext.RetryFaultedAsync(exception, context.CancellationToken).ConfigureAwait(false);

        if (_observers.Count > 0)
            await _observers.RetryFaultAsync(retryContext).ConfigureAwait(false);
    }
}
