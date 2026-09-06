using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>Carries state for batch consume operations.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class BatchConsumeContext<TMessage> :
    ConsumeContextScope,
    ConsumeContext<Batch<TMessage>>
    where TMessage : class
{
    readonly ConsumeContext _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="batch">The batch.</param>
    public BatchConsumeContext(ConsumeContext context, Batch<TMessage> batch)
        : base(context)
    {
        _context = context;
        Message = batch;
    }

    /// <summary>Reports that notify has been consumed.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return _context.NotifyConsumedAsync(context, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>Reports that notify has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    /// <summary>Gets the message.</summary>
    public Batch<TMessage> Message { get; }

    /// <summary>Reports that notify has been consumed.</summary>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return _context.NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>Reports that notify has faulted.</summary>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }
}
