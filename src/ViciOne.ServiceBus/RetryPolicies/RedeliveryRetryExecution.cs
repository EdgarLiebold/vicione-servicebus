using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Evaluates redelivery policy state without retrying observer or transport-lifecycle failures.</summary>
internal static class RedeliveryRetryExecution
{
    /// <summary>Processes one delivery and selects a redelivery decision for a handled business failure.</summary>
    /// <typeparam name="TContext">The consume or activity pipeline context type.</typeparam>
    /// <param name="context">The input delivery context.</param>
    /// <param name="next">The business pipeline to invoke.</param>
    /// <param name="policy">The redelivery policy.</param>
    /// <param name="observers">The policy lifecycle observers.</param>
    /// <param name="previousDeliveryCount">Reads the number of earlier redeliveries.</param>
    /// <param name="redeliver">Schedules the selected decision and acknowledges the current delivery.</param>
    /// <returns>A task that completes after processing, terminal failure, or redelivery scheduling.</returns>
    public static Task ExecuteAsync<TContext>(TContext context, IPipe<TContext> next, IRetryPolicy policy,
        RetryObservable observers, Func<int> previousDeliveryCount,
        Func<RetryContext<TContext>, Exception, CancellationToken, Task> redeliver)
        where TContext : class, PipeContext => RetryPolicyExecution.ExecuteAsync(context, policy,
            (policyContext, currentContext) => SendPolicyAsync(context, currentContext, next, policyContext,
                policy, observers, previousDeliveryCount, redeliver));

    [DebuggerNonUserCode]
    static async Task SendPolicyAsync<TContext>(TContext context, TContext currentContext, IPipe<TContext> next,
        RetryPolicyContext<TContext> policyContext, IRetryPolicy policy, RetryObservable observers,
        Func<int> previousDeliveryCount, Func<RetryContext<TContext>, Exception, CancellationToken, Task> redeliver)
        where TContext : class, PipeContext
    {
        if (observers.Count > 0)
            await RetryOperationState.ExecuteAsync(context, () => observers.PostCreateAsync(policyContext)).ConfigureAwait(false);

        try
        {
            await next.SendAsync(currentContext).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (RetryPolicyExecution.ShouldPropagate(context, currentContext, exception,
                    () => currentContext.CancellationToken))
                throw;

            if (!RetryPolicyExecution.CanRetry(context, policyContext, exception, out RetryContext<TContext> retryContext))
            {
                await NotifyTerminalAsync(context, policy, observers, retryContext, exception).ConfigureAwait(false);
                throw;
            }

            int previousCount = RetryPolicyExecution.Execute(context, previousDeliveryCount);
            for (int index = 0; index < previousCount; index++)
            {
                if (!RetryPolicyExecution.CanRetry(context, retryContext, exception, out RetryContext<TContext> decision))
                {
                    await NotifyTerminalAsync(context, policy, observers, decision, exception).ConfigureAwait(false);
                    throw;
                }
                retryContext = decision;
            }

            if (observers.Count > 0)
                await RetryOperationState.ExecuteAsync(context, () => observers.PostFaultAsync(retryContext)).ConfigureAwait(false);

            await RetryPolicyExecution.ExecuteLifecycleAsync(context, retryContext,
                token => redeliver(retryContext, exception, token)).ConfigureAwait(false);
        }
    }

    static async Task NotifyTerminalAsync<TContext>(TContext context, IRetryPolicy policy, RetryObservable observers,
        RetryContext<TContext> retryContext, Exception exception)
        where TContext : class, PipeContext
    {
        if (!RetryPolicyExecution.Execute(context, () => policy.IsHandled(exception)))
            return;

        RetryOperationState.PublishTerminal(context, retryContext);
        await RetryPolicyExecution.ExecuteLifecycleAsync(context, retryContext,
            token => retryContext.RetryFaultedAsync(exception, token)).ConfigureAwait(false);
        if (observers.Count > 0)
            await RetryOperationState.ExecuteAsync(context, () => observers.RetryFaultAsync(retryContext)).ConfigureAwait(false);
    }

    /// <summary>Schedules one redelivery, then separately acknowledges the original delivery.</summary>
    /// <param name="context">The delivery whose scheduling payload and transport metadata are required.</param>
    /// <param name="retryContext">The selected redelivery delay.</param>
    /// <param name="exception">The original business failure retained when scheduling fails.</param>
    /// <param name="notifyConsumed">Acknowledges the delivery after scheduling succeeds.</param>
    /// <param name="cancellationToken">The combined source and selected-decision token that cancels both stages.</param>
    /// <param name="callback">Optional outgoing-context customization applied by the redelivery provider.</param>
    /// <returns>A task that preserves scheduling or acknowledgment failure without scheduling again.</returns>
    public static async Task ScheduleAsync(ConsumeContext context, RetryContext retryContext,
        Exception exception, Func<CancellationToken, Task> notifyConsumed, CancellationToken cancellationToken,
        Action<ConsumeContext, SendContext>? callback = null)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            MessageRedeliveryContext redeliveryContext = context.GetPayload<MessageRedeliveryContext>();
            await redeliveryContext.ScheduleRedeliveryAsync(retryContext.Delay ?? TimeSpan.Zero, callback,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception redeliveryException)
        {
            throw new TransportException(context.ReceiveContext.InputAddress, "The message delivery could not be rescheduled",
                new AggregateException(redeliveryException, exception));
        }

        cancellationToken.ThrowIfCancellationRequested();
        Task notification = notifyConsumed(cancellationToken)
            ?? throw new InvalidOperationException("The redelivery acknowledgment returned a null task.");
        await notification.ConfigureAwait(false);
    }
}
