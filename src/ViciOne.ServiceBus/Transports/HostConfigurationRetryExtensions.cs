namespace ViciOne.ServiceBus.Transports
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration;
    using RetryPolicies;


    public static class HostConfigurationRetryExtensions
    {
        public static async Task Retry(this IHostConfiguration hostConfiguration, Func<Task> factory, CancellationToken cancellationToken,
            CancellationToken stoppingToken)
        {
            await Retry(hostConfiguration, factory, TimeProvider.System, cancellationToken, stoppingToken).ConfigureAwait(false);
        }

        public static async Task Retry(this IHostConfiguration hostConfiguration, Func<Task> factory, TimeProvider timeProvider,
            CancellationToken cancellationToken, CancellationToken stoppingToken)
        {
            ArgumentNullException.ThrowIfNull(hostConfiguration);
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentNullException.ThrowIfNull(timeProvider);

            var description = hostConfiguration.HostAddress;
            IRetryPolicy retryPolicy = hostConfiguration.SendTransportRetryPolicy
                ?? throw new InvalidOperationException("The host configuration returned a null send transport retry policy.");
            Exception lastFailure = null;

            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stoppingToken);

            try
            {
                if (stoppingToken.IsCancellationRequested)
                    throw CreateStoppingException(description, lastFailure);

                cancellationToken.ThrowIfCancellationRequested();

                await retryPolicy.Retry(async () =>
                {
                    if (stoppingToken.IsCancellationRequested)
                        throw CreateStoppingException(description, lastFailure);

                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await factory().ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        lastFailure = exception;
                        throw;
                    }
                }, timeProvider, linkedSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // A delay observes the linked token, not either source token. Source state is the
                // authoritative discriminator. A stopping transport is the stronger result when both
                // sources are cancelled; caller-only cancellation retains the exact caller token.
                if (stoppingToken.IsCancellationRequested)
                    throw CreateStoppingException(description, lastFailure);

                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        static ConnectionException CreateStoppingException(Uri description, Exception lastFailure)
        {
            return new ConnectionException($"The transport is stopping and cannot be used: {description}", lastFailure);
        }
    }
}
