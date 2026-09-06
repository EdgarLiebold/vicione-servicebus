using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Converts the ConsumeContext to a JobContext, and executes the job.</summary>
/// <typeparam name="TConsumer">The consumer type.</typeparam>
/// <typeparam name="TJob">The message type.</typeparam>
public class JobConsumerMessageFilter<TConsumer, TJob> :
    IConsumerMessageFilter<TConsumer, TJob>
    where TConsumer : class, IJobConsumer<TJob>
    where TJob : class
{
    readonly IRetryPolicy _retryPolicy;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="retryPolicy">The retry policy.</param>
    public JobConsumerMessageFilter(IRetryPolicy retryPolicy)
    {
        _retryPolicy = retryPolicy;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("consume");
        scope.Add("method", $"Consume(ConsumeContext<{TypeCache<TJob>.ShortName}> context)");
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(ConsumerConsumeContext<TConsumer, TJob> context,
        IPipe<ConsumerConsumeContext<TConsumer, TJob>> next)
    {
        if (context.Consumer is IJobConsumer<TJob> messageConsumer)
            return RunJobAsync(context, messageConsumer);

        var message = $"Consumer type {TypeCache<TConsumer>.ShortName} is not a consumer of job type {TypeCache<TJob>.ShortName}";

        throw new ConsumerMessageException(message);
    }

    async Task RunJobAsync(PipeContext context, IJobConsumer<TJob> jobConsumer)
    {
        var jobContext = context.GetPayload<JobContext<TJob>>();
        var notifyJobContext = context.GetPayload<INotifyJobContext>();

        RetryPolicyContext<JobContext<TJob>> policyContext = _retryPolicy.CreatePolicyContext(jobContext);

        try
        {
            await notifyJobContext.NotifyStartedAsync().ConfigureAwait(false);

            await jobConsumer.RunAsync(jobContext).ConfigureAwait(false);

            await notifyJobContext.NotifyCompletedAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (jobContext.CancellationToken == exception.CancellationToken)
        {
            await notifyJobContext.NotifyCanceledAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (!policyContext.CanRetry(exception, out RetryContext<JobContext<TJob>> retryContext))
            {
                if (_retryPolicy.IsHandled(exception))
                {
                    context.GetOrAddPayload(() => retryContext);

                    await retryContext.RetryFaultedAsync(exception).ConfigureAwait(false);
                }

                await notifyJobContext.NotifyFaultedAsync(exception).ConfigureAwait(false);
                return;
            }

            var currentRetryAttempt = jobContext.RetryAttempt;
            for (var retryIndex = 0; retryIndex < currentRetryAttempt; retryIndex++)
            {
                if (!retryContext.CanRetry(exception, out retryContext))
                {
                    if (_retryPolicy.IsHandled(exception))
                    {
                        context.GetOrAddPayload(() => retryContext);

                        await retryContext.RetryFaultedAsync(exception).ConfigureAwait(false);
                    }

                    await notifyJobContext.NotifyFaultedAsync(exception).ConfigureAwait(false);
                    return;
                }
            }

            var delay = retryContext.Delay ?? TimeSpan.Zero;

            await notifyJobContext.NotifyFaultedAsync(exception, delay).ConfigureAwait(false);
        }
        finally
        {
            policyContext.Dispose();
        }
    }
}
