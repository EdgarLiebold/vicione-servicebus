using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides extension methods for state machine introspection.</summary>
public static class StateMachineIntrospectionExtensions
{
    /// <summary>Returns the next state-machine events.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the next events outcome.</returns>
    public static Task<IEnumerable<IEvent>> NextEventsAsync<TInstance>(this IBehaviorContext<TInstance> context, CancellationToken cancellationToken = default)
        where TInstance : class, ISagaStateMachineInstance
    {
        ArgumentNullException.ThrowIfNull(context);

        return GetNextEventsAsync(context, cancellationToken);
    }

    static async Task<IEnumerable<IEvent>> GetNextEventsAsync<TInstance>(IBehaviorContext<TInstance> context, CancellationToken cancellationToken)
        where TInstance : class, ISagaStateMachineInstance
    {
        IStateMachine<TInstance> machine = context.StateMachine;
        var state = await machine.Accessor.GetAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The state machine accessor did not resolve a current state.");

        return machine.NextEvents(state);
    }
}
