using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the transition activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
internal sealed class TransitionActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    readonly IStateAccessor<TSaga> _currentStateAccessor;
    readonly IState<TSaga> _toState;

    /// <summary>Creates an activity that transitions a saga to a target state.</summary>
    /// <param name="toState">The target state.</param>
    /// <param name="currentStateAccessor">The accessor used to read and persist the current state.</param>
    public TransitionActivity(IState<TSaga> toState, IStateAccessor<TSaga> currentStateAccessor)
    {
        _toState = toState ?? throw new ArgumentNullException(nameof(toState));
        _currentStateAccessor = currentStateAccessor ?? throw new ArgumentNullException(nameof(currentStateAccessor));
    }

    /// <summary>Gets the target state.</summary>
    public IState ToState => _toState;

    /// <summary>Exposes this transition to a state-machine visitor.</summary>
    /// <param name="visitor">The visitor receiving the transition.</param>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    /// <summary>Writes the target-state name to a transition probe scope.</summary>
    /// <param name="context">The diagnostic context to populate.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateScope("transition");
        scope.Add("toState", _toState.Name);
    }

    /// <summary>Transitions the saga and then invokes the remaining behavior.</summary>
    /// <param name="context">The current saga behavior context.</param>
    /// <param name="next">The remaining behavior.</param>
    /// <returns>A task that completes after the transition and remaining behavior.</returns>
    public async Task ExecuteAsync(IBehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        await TransitionAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Transitions a data-event saga and then invokes the remaining behavior.</summary>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <param name="context">The current saga and event data.</param>
    /// <param name="next">The remaining data-event behavior.</param>
    /// <returns>A task that completes after the transition and remaining behavior.</returns>
    public async Task ExecuteAsync<TData>(IBehaviorContext<TSaga, TData> context, IBehavior<TSaga, TData> next)
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        await TransitionAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Forwards a state-machine fault to the remaining fault behavior.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The faulted saga behavior context.</param>
    /// <param name="next">The remaining fault behavior.</param>
    /// <returns>A task that completes after fault propagation.</returns>
    public Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.FaultedAsync(context);
    }

    /// <summary>Forwards a data-event fault to the remaining fault behavior.</summary>
    /// <typeparam name="T">The event data type.</typeparam>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <param name="context">The faulted saga and event data.</param>
    /// <param name="next">The remaining data-event fault behavior.</param>
    /// <returns>A task that completes after fault propagation.</returns>
    public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.FaultedAsync(context);
    }

    async Task TransitionAsync(IBehaviorContext<TSaga> context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        IState<TSaga>? currentState = await _currentStateAccessor
            .GetAsync(context, context.CancellationToken)
            .ConfigureAwait(false);
        if (_toState.Equals(currentState))
            return;

        if (currentState != null && !currentState.HasState(_toState))
            await RaiseCurrentStateLeaveEventsAsync(context, currentState, _toState).ConfigureAwait(false);

        await RaiseBeforeEnterEventsAsync(context, currentState, _toState).ConfigureAwait(false);

        await _currentStateAccessor.SetAsync(context, _toState, context.CancellationToken).ConfigureAwait(false);

        if (currentState != null)
            await RaiseAfterLeaveEventsAsync(context, currentState, _toState).ConfigureAwait(false);

        if (currentState == null || !_toState.HasState(currentState))
        {
            IState<TSaga>? superState = _toState.SuperState;
            while (superState != null && (currentState == null || !superState.HasState(currentState)))
            {
                IBehaviorContext<TSaga> superStateEnterContext = context.CreateProxy(superState.Enter);
                await superState.RaiseAsync(superStateEnterContext, context.CancellationToken).ConfigureAwait(false);

                superState = superState.SuperState;
            }

            IBehaviorContext<TSaga> enterContext = context.CreateProxy(_toState.Enter);
            await _toState.RaiseAsync(enterContext, context.CancellationToken).ConfigureAwait(false);
        }
    }

    static async Task RaiseBeforeEnterEventsAsync(IBehaviorContext<TSaga> context, IState<TSaga>? currentState, IState<TSaga> toState)
    {
        IState<TSaga>? superState = toState.SuperState;
        if (superState != null && (currentState == null || !superState.HasState(currentState)))
            await RaiseBeforeEnterEventsAsync(context, currentState, superState).ConfigureAwait(false);

        if (currentState != null && toState.HasState(currentState))
            return;

        IBehaviorContext<TSaga, IState> beforeContext = context.CreateProxy(toState.BeforeEnter, toState);
        await toState.RaiseAsync(beforeContext, context.CancellationToken).ConfigureAwait(false);
    }

    static async Task RaiseAfterLeaveEventsAsync(IBehaviorContext<TSaga> context, IState<TSaga> fromState, IState<TSaga> toState)
    {
        if (fromState.HasState(toState))
            return;

        IBehaviorContext<TSaga, IState> afterContext = context.CreateProxy(fromState.AfterLeave, fromState);
        await fromState.RaiseAsync(afterContext, context.CancellationToken).ConfigureAwait(false);

        IState<TSaga>? superState = fromState.SuperState;
        if (superState != null)
            await RaiseAfterLeaveEventsAsync(context, superState, toState).ConfigureAwait(false);
    }

    static async Task RaiseCurrentStateLeaveEventsAsync(IBehaviorContext<TSaga> context, IState<TSaga> fromState, IState<TSaga> toState)
    {
        IBehaviorContext<TSaga> leaveContext = context.CreateProxy(fromState.Leave);
        await fromState.RaiseAsync(leaveContext, context.CancellationToken).ConfigureAwait(false);

        IState<TSaga>? superState = fromState.SuperState;
        while (superState != null && !superState.HasState(toState))
        {
            IBehaviorContext<TSaga> superStateLeaveContext = context.CreateProxy(superState.Leave);
            await superState.RaiseAsync(superStateLeaveContext, context.CancellationToken).ConfigureAwait(false);

            superState = superState.SuperState;
        }
    }
}
