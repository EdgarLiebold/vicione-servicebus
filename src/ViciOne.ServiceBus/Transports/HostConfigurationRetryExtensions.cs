// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration;
    using Middleware;


    public static class HostConfigurationRetryExtensions
    {
        public static async Task Retry(this IHostConfiguration hostConfiguration, Func<Task> factory, CancellationToken cancellationToken,
            CancellationToken stoppingToken)
        {
            var description = hostConfiguration.HostAddress;

            using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stoppingToken);

            var stoppingContext = new SupervisorStoppingContext(tokenSource.Token);

            RetryPolicyContext<SupervisorStoppingContext> policyContext = hostConfiguration.SendTransportRetryPolicy.CreatePolicyContext(stoppingContext);

            try
            {
                RetryContext<SupervisorStoppingContext> retryContext = null;

                while (!tokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        if (retryContext?.Delay != null)
                            await Task.Delay(retryContext.Delay.Value, tokenSource.Token).ConfigureAwait(false);

                        if (stoppingToken.IsCancellationRequested)
                            throw new ConnectionException($"The transport is stopping and cannot be used: {description}", retryContext?.Exception);
                        if (cancellationToken.IsCancellationRequested)
                            cancellationToken.ThrowIfCancellationRequested();

                        await factory().ConfigureAwait(false);
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        // The backoff above waits on tokenSource.Token, which is linked from both
                        // sources, so a cancellation raised there carries the linked token and equals
                        // neither of them. Deciding from the exception's token therefore never reached
                        // the stopping branch, and a caller that cancelled its own publish received a
                        // TaskCanceledException bound to a token it had never seen. The sources are
                        // asked directly instead, in the same order and with the same outcome the two
                        // explicit checks in this method already use.
                        if (stoppingToken.IsCancellationRequested)
                            throw new ConnectionException($"The transport is stopping and cannot be used: {description}", retryContext?.Exception);

                        cancellationToken.ThrowIfCancellationRequested();

                        throw;
                    }
                    catch (Exception exception)
                    {
                        if (retryContext != null)
                        {
                            retryContext = retryContext.CanRetry(exception, out RetryContext<SupervisorStoppingContext> nextRetryContext)
                                ? nextRetryContext
                                : null;
                        }

                        if (retryContext == null && !policyContext.CanRetry(exception, out retryContext))
                            throw;
                    }

                    if (tokenSource.Token.IsCancellationRequested)
                        break;

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), tokenSource.Token).ConfigureAwait(false);
                    }
                    catch
                    {
                        // just a little breather before reconnecting the receive transport
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();

                throw new ConnectionException($"The transport is stopping and cannot be used: {description}", retryContext?.Exception);
            }
            finally
            {
                policyContext.Dispose();
            }
        }


        class SupervisorStoppingContext :
            BasePipeContext
        {
            public SupervisorStoppingContext(CancellationToken cancellationToken)
                : base(cancellationToken)
            {
            }
        }
    }
}
