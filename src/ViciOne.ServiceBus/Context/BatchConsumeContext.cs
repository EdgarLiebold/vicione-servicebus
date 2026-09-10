using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>Represents an assembled message batch while preserving the collector's consume context.</summary>
/// <typeparam name="TMessage">The batched message contract.</typeparam>
public class BatchConsumeContext<TMessage> :
    ConsumeContextScope,
    ConsumeContext<Batch<TMessage>>
    where TMessage : class
{
    readonly ConsumeContext _context;

    /// <summary>Creates a consume context for an assembled batch.</summary>
    /// <param name="context">The collector context that owns batch settlement.</param>
    /// <param name="batch">The ordered batch delivered to the consumer.</param>
    public BatchConsumeContext(ConsumeContext context, Batch<TMessage> batch)
        : base(context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        Message = batch ?? throw new ArgumentNullException(nameof(batch));
    }

    /// <summary>Forwards successful nested consumption to the collector context.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="context">The nested consume context that completed.</param>
    /// <param name="duration">The consumer execution duration.</param>
    /// <param name="consumerType">The diagnostic consumer name.</param>
    /// <param name="cancellationToken">Cancels observer notification.</param>
    /// <returns>A task that represents the collector context's consumed-message notification.</returns>
    public override Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return _context.NotifyConsumedAsync(context, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>Suppresses nested fault notification because the collector settles each original message.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="context">The nested consume context that faulted.</param>
    /// <param name="duration">The consumer execution duration.</param>
    /// <param name="consumerType">The diagnostic consumer name.</param>
    /// <param name="exception">The consumer failure.</param>
    /// <param name="cancellationToken">Cancels notification.</param>
    /// <returns>A completed task, or a canceled task when cancellation was requested.</returns>
    public override Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerType);
        ArgumentNullException.ThrowIfNull(exception);
        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled(cancellationToken)
            : Task.CompletedTask;
    }

    /// <summary>Gets the assembled batch.</summary>
    public Batch<TMessage> Message { get; }

    /// <summary>Forwards successful batch consumption to the collector context.</summary>
    /// <param name="duration">The batch-consumer execution duration.</param>
    /// <param name="consumerType">The diagnostic consumer name.</param>
    /// <param name="cancellationToken">Cancels observer notification.</param>
    /// <returns>A task that represents the collector context's consumed-batch notification.</returns>
    public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return _context.NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>Suppresses batch fault notification because the collector settles each original message.</summary>
    /// <param name="duration">The batch-consumer execution duration.</param>
    /// <param name="consumerType">The diagnostic consumer name.</param>
    /// <param name="exception">The consumer failure.</param>
    /// <param name="cancellationToken">Cancels notification.</param>
    /// <returns>A completed task, or a canceled task when cancellation was requested.</returns>
    public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerType);
        ArgumentNullException.ThrowIfNull(exception);
        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled(cancellationToken)
            : Task.CompletedTask;
    }
}
