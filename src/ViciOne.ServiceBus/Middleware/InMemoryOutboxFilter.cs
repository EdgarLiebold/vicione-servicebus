using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Runs a consume pipeline with deferred outgoing operations and scoped consume-context rebinding.</summary>
/// <typeparam name="TContext">The incoming pipeline context type.</typeparam>
/// <typeparam name="TResult">The outbox-decorated consume context passed to the downstream pipeline.</typeparam>
public class InMemoryOutboxFilter<TContext, TResult> :
    IFilter<TContext>
    where TContext : class, PipeContext
    where TResult : TContext, OutboxContext, ConsumeContext
{
    readonly bool _concurrentMessageDelivery;
    readonly Func<TContext, TResult> _contextFactory;
    /// <summary>
    /// Rebinds an existing consume scope to the outbox context when a setter is supplied.
    /// Without a setter or an IServiceScope payload, no scoped-context rebinding occurs.
    /// </summary>
    readonly ISetScopedConsumeContext? _setter;

    /// <summary>Configures outbox-context creation, optional scoped rebinding and deferred delivery concurrency.</summary>
    /// <param name="setter">The scoped consume-context setter, or null to leave scoped bindings unchanged.</param>
    /// <param name="contextFactory">The factory that decorates each incoming context with an outbox.</param>
    /// <param name="concurrentMessageDelivery">Whether independent deferred operations may execute concurrently.</param>
    public InMemoryOutboxFilter(ISetScopedConsumeContext? setter, Func<TContext, TResult> contextFactory, bool concurrentMessageDelivery)
    {
        _setter = setter;
        _contextFactory = contextFactory;
        _concurrentMessageDelivery = concurrentMessageDelivery;
    }

    /// <summary>Runs the downstream pipeline, executes deferred work and awaits consume completion.</summary>
    /// <remarks>
    /// A pipeline, deferred-delivery or completion failure triggers pending-work discard.
    /// The scoped-binding handle is disposed on exit. A discard or disposal failure can replace an earlier failure.
    /// </remarks>
    /// <param name="context">The incoming context to decorate with an outbox.</param>
    /// <param name="next">The downstream pipeline that receives the outbox context.</param>
    /// <returns>A task representing consumption and deferred delivery.</returns>
    public async Task SendAsync(TContext context, IPipe<TContext> next)
    {
        var outboxContext = _contextFactory(context);

        IDisposable? pop = null;
        if (_setter != null && context.TryGetPayload(out IServiceScope? scope))
            pop = _setter.PushContext(scope, outboxContext);

        try
        {
            await next.SendAsync(outboxContext).ConfigureAwait(false);

            await outboxContext.ExecutePendingActionsAsync(_concurrentMessageDelivery).ConfigureAwait(false);

            await outboxContext.ConsumeCompleted.ConfigureAwait(false);
        }
        catch (Exception)
        {
            await outboxContext.DiscardPendingActionsAsync().ConfigureAwait(false);

            throw;
        }
        finally
        {
            pop?.Dispose();
        }
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The probe context to enrich with the in-memory outbox filter.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("outbox");
        scope.Add("type", "in-memory");
    }
}
