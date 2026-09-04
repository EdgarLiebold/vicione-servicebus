using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a transition activity implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class TransitionActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly IStateAccessor<TSaga> _currentStateAccessor;
    readonly State<TSaga> _toState;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="toState">The to state value.</param>
    /// <param name="currentStateAccessor">The current state accessor value.</param>
    public TransitionActivity(State<TSaga> toState, IStateAccessor<TSaga> currentStateAccessor)
    {
        _toState = toState;
        _currentStateAccessor = currentStateAccessor;
    }

    /// <summary>
    /// Gets the to state value.
    /// </summary>
    public State ToState => _toState;

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="visitor">The visitor value.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("transition");
        scope.Add("toState", _toState.Name);
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        await TransitionAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExecuteAsync<TData>(BehaviorContext<TSaga, TData> context, IBehavior<TSaga, TData> next)
        where TData : class
    {
        await TransitionAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }

    async Task TransitionAsync(BehaviorContext<TSaga> context)
    {
        State<TSaga>? currentState = await _currentStateAccessor.GetAsync(context).ConfigureAwait(false);
        if (_toState.Equals(currentState))
            return; // Homey don't play re-entry, at least not yet.

        if (currentState != null && !currentState.HasState(_toState))
            await RaiseCurrentStateLeaveEventsAsync(context, currentState, _toState).ConfigureAwait(false);

        await RaiseBeforeEnterEventsAsync(context, currentState, _toState).ConfigureAwait(false);

        await _currentStateAccessor.SetAsync(context, _toState).ConfigureAwait(false);

        if (currentState != null)
            await RaiseAfterLeaveEventsAsync(context, currentState, _toState).ConfigureAwait(false);

        if (currentState == null || !_toState.HasState(currentState))
        {
            State<TSaga>? superState = _toState.SuperState;
            while (superState != null && (currentState == null || !superState.HasState(currentState)))
            {
                BehaviorContext<TSaga> superStateEnterContext = context.CreateProxy(superState.Enter);
                await superState.RaiseAsync(superStateEnterContext).ConfigureAwait(false);

                superState = superState.SuperState;
            }

            BehaviorContext<TSaga> enterContext = context.CreateProxy(_toState.Enter);
            await _toState.RaiseAsync(enterContext).ConfigureAwait(false);
        }
    }

    static async Task RaiseBeforeEnterEventsAsync(BehaviorContext<TSaga> context, State<TSaga>? currentState, State<TSaga> toState)
    {
        State<TSaga>? superState = toState.SuperState;
        if (superState != null && (currentState == null || !superState.HasState(currentState)))
            await RaiseBeforeEnterEventsAsync(context, currentState, superState).ConfigureAwait(false);

        if (currentState != null && toState.HasState(currentState))
            return;

        BehaviorContext<TSaga, State> beforeContext = context.CreateProxy(toState.BeforeEnter, toState);
        await toState.RaiseAsync(beforeContext).ConfigureAwait(false);
    }

    static async Task RaiseAfterLeaveEventsAsync(BehaviorContext<TSaga> context, State<TSaga> fromState, State<TSaga> toState)
    {
        if (fromState.HasState(toState))
            return;

        BehaviorContext<TSaga, State> afterContext = context.CreateProxy(fromState.AfterLeave, fromState);
        await fromState.RaiseAsync(afterContext).ConfigureAwait(false);

        State<TSaga>? superState = fromState.SuperState;
        if (superState != null)
            await RaiseAfterLeaveEventsAsync(context, superState, toState).ConfigureAwait(false);
    }

    static async Task RaiseCurrentStateLeaveEventsAsync(BehaviorContext<TSaga> context, State<TSaga> fromState, State<TSaga> toState)
    {
        BehaviorContext<TSaga> leaveContext = context.CreateProxy(fromState.Leave);
        await fromState.RaiseAsync(leaveContext).ConfigureAwait(false);

        State<TSaga>? superState = fromState.SuperState;
        while (superState != null && !superState.HasState(toState))
        {
            BehaviorContext<TSaga> superStateLeaveContext = context.CreateProxy(superState.Leave);
            await superState.RaiseAsync(superStateLeaveContext).ConfigureAwait(false);

            superState = superState.SuperState;
        }
    }
}
