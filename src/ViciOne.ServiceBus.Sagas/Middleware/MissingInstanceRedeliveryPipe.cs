using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Middleware;

internal sealed class MissingInstanceRedeliveryPipe<TSaga, TMessage> :
    IPipe<ConsumeContext<TMessage>>
    where TSaga : ISagaStateMachineInstance
    where TMessage : class
{
    // Broker metadata is untrusted: this ceiling keeps policy reconstruction finite while retaining ample retry history.
    const int MaximumPreviousDeliveryCount = 10_000;
    const RedeliveryOptions SupportedOptions = RedeliveryOptions.ReplaceMessageId | RedeliveryOptions.ConfigureMessageScheduler;

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
        if ((options & ~SupportedOptions) != 0)
            throw new ArgumentOutOfRangeException(nameof(options), options, "The redelivery options contain unsupported flags.");

        _options = options;
    }

    public async Task SendAsync(ConsumeContext<TMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CancellationToken.ThrowIfCancellationRequested();

        RetryPolicyContext<ConsumeContext<TMessage>> policyContext = _retryPolicy.CreatePolicyContext(context)
            ?? throw new InvalidOperationException("The retry policy returned a null policy context.");
        Exception? operationFailure = null;
        try
        {
            await ExecuteAsync(context, policyContext).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            operationFailure = exception;
        }

        Exception? disposalFailure = null;
        try
        {
            policyContext.Dispose();
        }
        catch (Exception exception)
        {
            disposalFailure = exception;
        }

        if (operationFailure is not null && disposalFailure is not null)
        {
            throw new AggregateException(
                "The missing-instance redelivery operation and policy-context disposal failed.",
                operationFailure,
                disposalFailure);
        }

        if (operationFailure is not null)
            ExceptionDispatchInfo.Throw(operationFailure);

        if (disposalFailure is not null)
            ExceptionDispatchInfo.Throw(disposalFailure);
    }

    async Task ExecuteAsync(
        ConsumeContext<TMessage> context,
        RetryPolicyContext<ConsumeContext<TMessage>> policyContext)
    {
        if (policyContext.Context == null)
            throw new InvalidOperationException("The retry policy returned a policy context without a consume context.");

        context.CancellationToken.ThrowIfCancellationRequested();

        int previousDeliveryCount = context.Advanced().GetRedeliveryCount();
        if (previousDeliveryCount is < 0 or > MaximumPreviousDeliveryCount)
        {
            throw new InvalidOperationException(
                $"The previous redelivery count must be between 0 and {MaximumPreviousDeliveryCount}.");
        }

        if (_observers.Count > 0)
            await _observers.PostCreateAsync(policyContext).ConfigureAwait(false);

        context.CancellationToken.ThrowIfCancellationRequested();

        var exception = context.CorrelationId is { } correlationId
            ? new SagaException("An existing saga instance was not found", typeof(TSaga), typeof(TMessage), correlationId)
            : new SagaException("An existing saga instance was not found", typeof(TSaga), typeof(TMessage));

        if (!policyContext.CanRetry(exception, out RetryContext<ConsumeContext<TMessage>> retryContext))
        {
            EnsureRetryContext(retryContext);
            await NotifyRetryFaultAsync(context, retryContext, exception).ConfigureAwait(false);
            context.CancellationToken.ThrowIfCancellationRequested();
            await _finalPipe.SendAsync(context).ConfigureAwait(false);
            return;
        }

        EnsureRetryContext(retryContext);
        for (var retryIndex = 0; retryIndex < previousDeliveryCount; retryIndex++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (!retryContext.CanRetry(exception, out retryContext))
            {
                EnsureRetryContext(retryContext);
                await NotifyRetryFaultAsync(context, retryContext, exception).ConfigureAwait(false);
                context.CancellationToken.ThrowIfCancellationRequested();
                await _finalPipe.SendAsync(context).ConfigureAwait(false);
                return;
            }

            EnsureRetryContext(retryContext);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        if (_observers.Count > 0)
            await _observers.PostFaultAsync(retryContext).ConfigureAwait(false);

        context.CancellationToken.ThrowIfCancellationRequested();
        var redeliveryContext = _options.HasFlag(RedeliveryOptions.ConfigureMessageScheduler)
            ? (MessageRedeliveryContext)new ScheduleMessageRedeliveryContext<TMessage>(context, _options)
            : new DelayedMessageRedeliveryContext<TMessage>(context, _options);

        var delay = retryContext.Delay ?? TimeSpan.Zero;

        await redeliveryContext.ScheduleRedeliveryAsync(delay, cancellationToken: context.CancellationToken).ConfigureAwait(false);
    }

    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateScope("missingInstanceRedelivery");
        scope.Add("sagaType", TypeCache<TSaga>.ShortName);
        scope.Add("messageType", TypeCache<TMessage>.ShortName);
        scope.Add("replaceMessageId", _options.HasFlag(RedeliveryOptions.ReplaceMessageId));
        scope.Add("configureMessageScheduler", _options.HasFlag(RedeliveryOptions.ConfigureMessageScheduler));
        scope.Add("maximumPreviousDeliveryCount", MaximumPreviousDeliveryCount);
        _retryPolicy.Probe(scope);
        _finalPipe.Probe(scope);
    }

    async Task NotifyRetryFaultAsync(
        ConsumeContext<TMessage> context,
        RetryContext<ConsumeContext<TMessage>> retryContext,
        Exception exception)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        if (!_retryPolicy.IsHandled(exception))
            return;

        context.CancellationToken.ThrowIfCancellationRequested();
        context.GetOrAddPayload(() => retryContext);
        await retryContext.RetryFaultedAsync(exception, context.CancellationToken).ConfigureAwait(false);

        context.CancellationToken.ThrowIfCancellationRequested();
        if (_observers.Count > 0)
            await _observers.RetryFaultAsync(retryContext).ConfigureAwait(false);
    }

    static void EnsureRetryContext(RetryContext<ConsumeContext<TMessage>>? retryContext)
    {
        if (retryContext is null)
            throw new InvalidOperationException("The retry policy returned a null retry context.");
    }
}
