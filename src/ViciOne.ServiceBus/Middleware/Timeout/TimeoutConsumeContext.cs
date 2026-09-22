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
        : base(context.Advanced())
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The timeout must be greater than zero.");

        CancellationToken = cancellationToken;
        _context = context;
        _timeout = timeout;
    }

    public override CancellationToken CancellationToken { get; }

    public TMessage Message => _context.Message;

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
        bool usesTimeoutContext = ReferenceEquals(context, this)
            || context.TryGetPayload<TimeoutConsumeContext<TMessage>>(out var timeoutContext)
            && ReferenceEquals(timeoutContext, this);
        CancellationToken messageCancellationToken = usesTimeoutContext
            ? _context.CancellationToken
            : context.CancellationToken;

        switch (exception)
        {
            case OperationCanceledException canceledException when canceledException.CancellationToken == messageCancellationToken:
                break;

            default:
                if (!messageCancellationToken.IsCancellationRequested)
                {
                    if (exception is OperationCanceledException timeoutException
                        && timeoutException.CancellationToken == CancellationToken
                        && CancellationToken.IsCancellationRequested)
                    {
                        reportedException = new ConsumerCanceledException(
                            $"The operation exceeded the configured timeout of {_timeout}.",
                            timeoutException);
                    }

                    Task generation = (usesTimeoutContext
                        ? GenerateFaultAsync(_context, reportedException)
                        : GenerateFaultAsync(context, reportedException))
                        ?? throw new InvalidOperationException("The consume context returned no fault-generation task.");
                    await generation.ConfigureAwait(false);
                }
                break;
        }

        Task notification = ReceiveContext.NotifyFaultedAsync(context, duration, consumerType, reportedException, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The receive context returned no consume-fault notification task.");
        await notification.ConfigureAwait(false);
    }

    public virtual Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
    }

    public virtual Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return NotifyFaultedAsync(this, duration, consumerType, exception, cancellationToken: cancellationToken);
    }
}
