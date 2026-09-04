using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;

namespace ViciOne.ServiceBus.Middleware;

public class InMemoryOutboxFilter<TContext, TResult> :
    IFilter<TContext>
    where TContext : class, PipeContext
    where TResult : TContext, OutboxContext, ConsumeContext
{
    readonly bool _concurrentMessageDelivery;
    readonly Func<TContext, TResult> _contextFactory;
    /// <summary>
    /// The bus-bound setter, or nothing at all. The direct configuration has no container, so there
    /// is no bus-bound scoped context to rebind and this stays absent; the filter then leaves the
    /// consume context exactly as it found it instead of reaching into some other provider for one.
    /// </summary>
    readonly ISetScopedConsumeContext? _setter;

    public InMemoryOutboxFilter(ISetScopedConsumeContext? setter, Func<TContext, TResult> contextFactory, bool concurrentMessageDelivery)
    {
        _setter = setter;
        _contextFactory = contextFactory;
        _concurrentMessageDelivery = concurrentMessageDelivery;
    }

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

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("outbox");
        scope.Add("type", "in-memory");
    }
}
