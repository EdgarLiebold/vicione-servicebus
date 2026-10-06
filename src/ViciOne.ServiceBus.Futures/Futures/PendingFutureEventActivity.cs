using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Accepts tracked outcomes before any event activities can mutate a future or invoke user callbacks.</summary>
internal sealed class PendingFutureEventActivity<TMessage> : IStateMachineActivity<FutureState, TMessage>
    where TMessage : class
{
    readonly Func<IBehaviorContext<FutureState, TMessage>, Task<bool>> _isNonTerminal;
    readonly List<PendingFutureIdProvider<TMessage>> _pendingIdProviders = [];

    public PendingFutureEventActivity(Func<IBehaviorContext<FutureState, TMessage>, Task<bool>> isNonTerminal)
    {
        _isNonTerminal = isNonTerminal ?? throw new ArgumentNullException(nameof(isNonTerminal));
    }

    public void AddPendingIdProvider(PendingFutureIdProvider<TMessage> provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _pendingIdProviders.Add(provider);
    }

    public async Task ExecuteAsync(IBehaviorContext<FutureState, TMessage> context, IBehavior<FutureState, TMessage> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        if (_pendingIdProviders.Count != 0)
        {
            if (!await _isNonTerminal(context).ConfigureAwait(false))
                return;

            bool pending = false;
            foreach (PendingFutureIdProvider<TMessage> provider in _pendingIdProviders)
            {
                if (context.Saga.Pending.Contains(provider(context.Message)))
                {
                    pending = true;
                    break;
                }
            }
            if (!pending)
                return;
        }

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    public Task FaultedAsync<TException>(IBehaviorExceptionContext<FutureState, TMessage, TException> context,
        IBehavior<FutureState, TMessage> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.FaultedAsync(context);
    }

    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateScope("pendingFutureEvent");
    }

    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }
}
