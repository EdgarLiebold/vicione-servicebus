using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Provides extension methods for state accessor.
/// </summary>
public static class StateAccessorExtensions
{
    /// <summary>
    /// Gets state.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="accessor">The accessor value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<State<TSaga>?> GetStateAsync<TSaga>(this IStateAccessor<TSaga> accessor, BehaviorContext<TSaga> context, CancellationToken cancellationToken = default)
        where TSaga : class, SagaStateMachineInstance
    {
        return accessor.GetAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets state.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="accessor">The accessor value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<State<TSaga>?> GetStateAsync<TSaga>(this StateMachine<TSaga> accessor, BehaviorContext<TSaga> context, CancellationToken cancellationToken = default)
        where TSaga : class, SagaStateMachineInstance
    {
        return accessor.Accessor.GetAsync(context, cancellationToken: cancellationToken);
    }
}
