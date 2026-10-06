using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes timeout pipeline stages.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
public sealed class TimeoutFilter<TContext, TResult> :
    IFilter<TContext>
    where TContext : class, PipeContext
    where TResult : TContext
{
    readonly Func<TContext, CancellationToken, TResult> _contextFactory;
    readonly TimeProvider? _timeProvider;
    readonly TimeSpan _timeout;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="contextFactory">The context factory.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    public TimeoutFilter(Func<TContext, CancellationToken, TResult> contextFactory, TimeSpan timeout)
        : this(contextFactory, timeout, null, useContextTimeProvider: true)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="contextFactory">The context factory.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public TimeoutFilter(Func<TContext, CancellationToken, TResult> contextFactory, TimeSpan timeout, TimeProvider timeProvider)
        : this(contextFactory, timeout, timeProvider, useContextTimeProvider: false)
    {
    }

    TimeoutFilter(Func<TContext, CancellationToken, TResult> contextFactory, TimeSpan timeout, TimeProvider? timeProvider,
        bool useContextTimeProvider)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The timeout must be greater than zero.");
        if (!useContextTimeProvider)
            ArgumentNullException.ThrowIfNull(timeProvider);

        _contextFactory = contextFactory;
        _timeProvider = timeProvider;
        _timeout = timeout;
    }

    /// <summary>Runs the continuation with a linked timeout context and awaits its consume completion.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(TContext context, IPipe<TContext> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        CancellationToken callerToken = context.CancellationToken;
        using var timeoutSource = new CancellationTokenSource(_timeout, _timeProvider ?? context.GetTimeProvider());
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(callerToken, timeoutSource.Token);

        try
        {
            TResult timeoutContext = _contextFactory(context, linkedSource.Token)
                ?? throw new InvalidOperationException("The timeout context factory returned null.");

            await next.SendAsync(timeoutContext).ConfigureAwait(false);

            if (timeoutContext is not ConsumeContext consumeContext)
                throw new InvalidOperationException("The timeout context does not expose consume completion state.");

            await consumeContext.ConsumeCompleted.ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (callerToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("The operation was canceled by the caller.", exception, callerToken);
        }
        catch (OperationCanceledException exception) when (!callerToken.IsCancellationRequested
            && timeoutSource.IsCancellationRequested
            && (exception.CancellationToken == linkedSource.Token || exception.CancellationToken == timeoutSource.Token))
        {
            throw new ConsumerCanceledException($"The operation exceeded the configured timeout of {_timeout}.", exception);
        }
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("timeout");
        scope.Add("timeout", _timeout);
    }
}
