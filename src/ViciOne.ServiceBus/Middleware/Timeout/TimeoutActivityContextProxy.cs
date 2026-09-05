using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware.Timeout;

internal abstract class TimeoutActivityContextProxy :
    ActivityContextProxy
{
    readonly ActivityContext _activityContext;
    readonly TimeSpan _timeout;

    protected TimeoutActivityContextProxy(ActivityContext activityContext, CancellationToken cancellationToken, TimeSpan timeout)
        : base(activityContext)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The timeout must be greater than zero.");

        _activityContext = activityContext;
        CancellationToken = cancellationToken;
        _timeout = timeout;
    }

    public override CancellationToken CancellationToken { get; }

    public override async Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception,
        CancellationToken cancellationToken = default)
    {
        Exception reportedException = exception;

        if (exception is not OperationCanceledException canceledException
            || canceledException.CancellationToken != _activityContext.CancellationToken)
        {
            if (!_activityContext.CancellationToken.IsCancellationRequested)
            {
                if (exception is OperationCanceledException timeoutException && CancellationToken.IsCancellationRequested)
                {
                    reportedException = new ConsumerCanceledException(
                        $"The operation exceeded the configured timeout of {_timeout}.", timeoutException);
                }

                await GenerateFaultAsync(context, reportedException).ConfigureAwait(false);
            }
        }

        await ReceiveContext.NotifyFaultedAsync(context, duration, consumerType, reportedException, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
