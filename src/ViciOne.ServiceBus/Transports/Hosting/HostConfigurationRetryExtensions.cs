using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Executes transport operations through a host's configured send retry policy.</summary>
public static class HostConfigurationRetryExtensions
{
    /// <summary>Retries an operation using the system time provider.</summary>
    /// <param name="hostConfiguration">The host configuration that supplies the retry policy and transport address.</param>
    /// <param name="factory">The operation to execute.</param>
    /// <param name="stoppingToken">The token signaled when the transport is stopping.</param>
    /// <param name="cancellationToken">The token that cancels the caller's operation.</param>
    /// <returns>A task that completes when the operation succeeds.</returns>
    public static async Task RetryAsync(this IHostConfiguration hostConfiguration, Func<Task> factory, CancellationToken stoppingToken,
        CancellationToken cancellationToken = default)
    {
        await RetryAsync(hostConfiguration, factory, TimeProvider.System, stoppingToken, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retries an operation using an explicit time provider.</summary>
    /// <param name="hostConfiguration">The host configuration that supplies the retry policy and transport address.</param>
    /// <param name="factory">The operation to execute.</param>
    /// <param name="timeProvider">The time source used for retry delays.</param>
    /// <param name="stoppingToken">The token signaled when the transport is stopping.</param>
    /// <param name="cancellationToken">The token that cancels the caller's operation.</param>
    /// <returns>A task that completes when the operation succeeds.</returns>
    public static async Task RetryAsync(this IHostConfiguration hostConfiguration, Func<Task> factory, TimeProvider timeProvider,
        CancellationToken stoppingToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var description = hostConfiguration.HostAddress;
        IRetryPolicy retryPolicy = hostConfiguration.SendTransportRetryPolicy
            ?? throw new InvalidOperationException("The host configuration returned a null send transport retry policy.");
        Exception? lastFailure = null;

        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stoppingToken);

        try
        {
            if (stoppingToken.IsCancellationRequested)
                throw CreateStoppingException(description, lastFailure);

            cancellationToken.ThrowIfCancellationRequested();

            await retryPolicy.RetryAsync(async () =>
            {
                if (stoppingToken.IsCancellationRequested)
                    throw CreateStoppingException(description, lastFailure);

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    Task operation = factory()
                        ?? throw new InvalidOperationException("The retried transport operation returned no task.");
                    await operation.ConfigureAwait(false);
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

    static ConnectionException CreateStoppingException(Uri description, Exception? lastFailure)
    {
        return new ConnectionException(
            $"The transport is stopping and cannot be used: {description}",
            lastFailure,
            isTransient: true);
    }
}
