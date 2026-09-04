using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Provides extension methods for state machine.
/// </summary>
public static class StateMachineExtensions
{
    /// <summary>
    /// Transition a state machine instance to a specific state, producing any events related
    /// to the transaction such as leaving the previous state and entering the target state
    /// </summary>
    /// <typeparam name="TSaga">The state instance type</typeparam>
    /// <param name="context"></param>
    /// <param name="state">The target state</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task TransitionToStateAsync<TSaga>(this BehaviorContext<TSaga> context, State state, CancellationToken cancellationToken = default)
        where TSaga : class, SagaStateMachineInstance
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); IStateAccessor<TSaga> accessor = context.StateMachine.Accessor;
        State<TSaga> toState = context.StateMachine.GetState(state.Name);

        IStateMachineActivity<TSaga> activity = new TransitionActivity<TSaga>(toState, accessor);
        IBehavior<TSaga> behavior = new LastBehavior<TSaga>(activity);

        return behavior.ExecuteAsync(context.CreateProxy(toState.Enter));
    }
}
