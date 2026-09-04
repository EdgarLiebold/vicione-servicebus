using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware.Timeout;

internal class TimeoutConsumeContext<TMessage> :
    ConsumeContextProxy,
    ConsumeContext<TMessage>
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;
    readonly TimeSpan _timeout;

    public TimeoutConsumeContext(ConsumeContext<TMessage> context, CancellationToken cancellationToken, TimeSpan timeout)
        : base(context)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The timeout must be greater than zero.");

        CancellationToken = cancellationToken;
        _context = context;
        _timeout = timeout;
    }

    public override CancellationToken CancellationToken { get; }

    public TMessage Message => _context.Message;

    public override async Task NotifyFaulted<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
    {
        Exception reportedException = exception;

        switch (exception)
        {
            case OperationCanceledException canceledException when canceledException.CancellationToken == _context.CancellationToken:
                break;

            default:
                if (!_context.CancellationToken.IsCancellationRequested)
                {
                    if (exception is OperationCanceledException timeoutException
                        && CancellationToken.IsCancellationRequested)
                    {
                        reportedException = new ConsumerCanceledException(
                            $"The operation exceeded the configured timeout of {_timeout}.",
                            timeoutException);
                    }

                    await GenerateFault(_context, reportedException).ConfigureAwait(false);
                }
                break;
        }

        await ReceiveContext.NotifyFaulted(context, duration, consumerType, reportedException).ConfigureAwait(false);
    }

    public virtual Task NotifyConsumed(TimeSpan duration, string consumerType)
    {
        return NotifyConsumed(this, duration, consumerType);
    }

    public virtual Task NotifyFaulted(TimeSpan duration, string consumerType, Exception exception)
    {
        return NotifyFaulted(this, duration, consumerType, exception);
    }
}
