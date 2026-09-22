using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware.Timeout;

internal abstract class TimeoutActivityContextProxy :
    ActivityContextProxy
{
    readonly TimeSpan _timeout;

    protected TimeoutActivityContextProxy(ActivityContext activityContext, CancellationToken cancellationToken, TimeSpan timeout)
        : base(activityContext)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The timeout must be greater than zero.");

        CancellationToken = cancellationToken;
        _timeout = timeout;
    }

    public override CancellationToken CancellationToken { get; }

    public override Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerType);
        ArgumentNullException.ThrowIfNull(exception);

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return NotifyFaultedCoreAsync(context, duration, consumerType, exception, cancellationToken);
    }

    async Task NotifyFaultedCoreAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception,
        CancellationToken cancellationToken)
        where T : class
    {
        Exception reportedException = exception;
        CancellationToken messageCancellationToken = context.CancellationToken;

        if (exception is not OperationCanceledException canceledException
            || canceledException.CancellationToken != messageCancellationToken)
        {
            if (!messageCancellationToken.IsCancellationRequested)
            {
                if (exception is OperationCanceledException timeoutException
                    && timeoutException.CancellationToken == CancellationToken
                    && CancellationToken.IsCancellationRequested)
                {
                    reportedException = new ConsumerCanceledException(
                        $"The operation exceeded the configured timeout of {_timeout}.", timeoutException);
                }

                Task generation = GenerateFaultAsync(context, reportedException)
                    ?? throw new InvalidOperationException("The consume context returned no fault-generation task.");
                await generation.ConfigureAwait(false);
            }
        }

        Task notification = ReceiveContext.NotifyFaultedAsync(context, duration, consumerType, reportedException, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The receive context returned no consume-fault notification task.");
        await notification.ConfigureAwait(false);
    }
}
