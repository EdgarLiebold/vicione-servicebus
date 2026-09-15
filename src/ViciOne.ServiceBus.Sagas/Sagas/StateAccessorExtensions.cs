using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Reads saga states through a state accessor or its owning state machine.</summary>
public static class StateAccessorExtensions
{
    /// <summary>Reads the saga's current state through the supplied accessor.</summary>
    /// <remarks>The accessor may initialize a missing state as part of the read.</remarks>
    /// <typeparam name="TSaga">The saga instance type.</typeparam>
    /// <param name="accessor">The state accessor to invoke.</param>
    /// <param name="context">The behavior context containing the saga instance.</param>
    /// <param name="cancellationToken">The cancellation token forwarded to the accessor.</param>
    /// <returns>A task containing the reported current state, or null if the accessor reports no state.</returns>
    public static Task<IState<TSaga>?> GetStateAsync<TSaga>(this IStateAccessor<TSaga> accessor, IBehaviorContext<TSaga> context, CancellationToken cancellationToken = default)
        where TSaga : class, ISagaStateMachineInstance
    {
        return accessor.GetAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>Reads the saga's current state through the machine's configured accessor.</summary>
    /// <remarks>The configured accessor may initialize a missing state as part of the read.</remarks>
    /// <typeparam name="TSaga">The saga instance type.</typeparam>
    /// <param name="accessor">The state machine whose accessor reads the state.</param>
    /// <param name="context">The behavior context containing the saga instance.</param>
    /// <param name="cancellationToken">The cancellation token forwarded to the configured accessor.</param>
    /// <returns>A task containing the reported current state, or null if the accessor reports no state.</returns>
    public static Task<IState<TSaga>?> GetStateAsync<TSaga>(this IStateMachine<TSaga> accessor, IBehaviorContext<TSaga> context, CancellationToken cancellationToken = default)
        where TSaga : class, ISagaStateMachineInstance
    {
        return accessor.Accessor.GetAsync(context, cancellationToken: cancellationToken);
    }
}
