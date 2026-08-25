#nullable enable
namespace ViciOne.ServiceBus.Middleware;

using System;
using System.Threading.Tasks;
using CircuitBreaker;

internal sealed class CircuitBreakerFilter<TContext> : IFilter<TContext>
    where TContext : class, PipeContext
{
    private readonly CircuitBreakerSettings _settings;
    private readonly CircuitBreakerStateMachine _stateMachine;

    public CircuitBreakerFilter(CircuitBreakerSettings settings)
    {
        _settings = settings;
        _stateMachine = new CircuitBreakerStateMachine(settings);
    }

    public async Task Send(TContext context, IPipe<TContext> next)
    {
        CircuitBreakerLease lease = _stateMachine.Acquire();
        try
        {
            await next.Send(context).ConfigureAwait(false);
            _stateMachine.RecordSuccess(lease);
        }
        catch (Exception exception)
        {
            if (IsCallerCancellation(context, exception) || !_settings.ExceptionFilter.Match(exception))
                _stateMachine.ReleaseWithoutVerdict(lease);
            else
                _stateMachine.RecordFailure(lease, exception);

            throw;
        }
    }

    void IProbeSite.Probe(ProbeContext context)
    {
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
        if (exception is not OperationCanceledException cancellation || !context.CancellationToken.CanBeCanceled)
            return false;

        return context.CancellationToken.IsCancellationRequested
            || cancellation.CancellationToken == context.CancellationToken;
    }
}
