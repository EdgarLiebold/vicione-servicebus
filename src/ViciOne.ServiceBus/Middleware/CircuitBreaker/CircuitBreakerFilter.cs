using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.CircuitBreaker;

internal sealed class CircuitBreakerFilter<TContext> : IFilter<TContext>
    where TContext : class, PipeContext
{
    private readonly CircuitBreakerSettings _settings;
    private readonly CircuitBreakerStateMachine _stateMachine;

    public CircuitBreakerFilter(CircuitBreakerSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _stateMachine = new CircuitBreakerStateMachine(settings);
    }

    public async Task SendAsync(TContext context, IPipe<TContext> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        CircuitBreakerLease lease = _stateMachine.Acquire();
        try
        {
            await next.SendAsync(context).ConfigureAwait(false);
            _stateMachine.RecordSuccess(lease);
        }
        catch (Exception exception)
        {
            bool isClassifiedFailure;
            try
            {
                isClassifiedFailure = !IsCallerCancellation(context, exception)
                    && _settings.ExceptionFilter.Match(exception);
            }
            catch
            {
                _stateMachine.ReleaseWithoutVerdict(lease);
                throw;
            }

            if (isClassifiedFailure)
                _stateMachine.RecordFailure(lease, exception);
            else
                _stateMachine.ReleaseWithoutVerdict(lease);

            throw;
        }
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        CircuitBreakerSnapshot snapshot = _stateMachine.GetSnapshot();
        var scope = context.CreateFilterScope("circuitBreaker");
        scope.Set(new
        {
            _settings.MinimumThroughput,
            _settings.FailureRatio,
            _settings.SamplingDuration,
            BreakDurations = (TimeSpan[])_settings.BreakDurations.Clone(),
            snapshot.State,
            snapshot.AttemptCount,
            snapshot.FailureCount,
            snapshot.RetryAfter,
            snapshot.ProbeInProgress,
        });
    }

    private static bool IsCallerCancellation(TContext context, Exception exception)
    {
        if (exception is not OperationCanceledException canceled
            || !context.CancellationToken.IsCancellationRequested)
            return false;

        return !canceled.CancellationToken.CanBeCanceled
            || canceled.CancellationToken == context.CancellationToken;
    }
}
