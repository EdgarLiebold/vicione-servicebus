using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Executes a job consumer with job lifecycle notifications and retry-state evaluation.</summary>
/// <typeparam name="TConsumer">The consumer type.</typeparam>
/// <typeparam name="TJob">The job contract type.</typeparam>
internal sealed class JobConsumerMessageFilter<TConsumer, TJob> :
    IConsumerMessageFilter<TConsumer, TJob>
    where TConsumer : class, IJobConsumer<TJob>
    where TJob : class
{
    readonly IRetryPolicy _retryPolicy;

    /// <summary>Initializes a filter with the retry policy configured for the job consumer.</summary>
    /// <param name="retryPolicy">The policy used to decide whether a failed attempt may be retried.</param>
    public JobConsumerMessageFilter(IRetryPolicy retryPolicy)
    {
        ArgumentNullException.ThrowIfNull(retryPolicy);

        _retryPolicy = retryPolicy;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateScope("consume");
        scope.Add("method", $"RunAsync(JobContext<{TypeCache<TJob>.ShortName}> context)");
    }

    /// <summary>Runs the terminal job-consumer method with lifecycle and retry notifications.</summary>
    /// <param name="context">The resolved consumer and job context.</param>
    /// <param name="next">The terminal filter continuation required by the consumer-pipeline contract.</param>
    /// <returns>The job execution and notification task.</returns>
    public Task SendAsync(ConsumerConsumeContext<TConsumer, TJob> context,
        IPipe<ConsumerConsumeContext<TConsumer, TJob>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context.Consumer is IJobConsumer<TJob> jobConsumer)
            return RunJobAsync(context, jobConsumer);

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
