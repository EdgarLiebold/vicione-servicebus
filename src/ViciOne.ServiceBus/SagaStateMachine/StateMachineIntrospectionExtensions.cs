using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Provides extension methods for state machine introspection.
/// </summary>
public static class StateMachineIntrospectionExtensions
{
    /// <summary>
    /// Performs the next events operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<IEnumerable<Event>> NextEventsAsync<TInstance>(this BehaviorContext<TInstance> context, CancellationToken cancellationToken = default)
        where TInstance : class, SagaStateMachineInstance
    {
        var state = await context.StateMachine.Accessor.GetAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The state machine accessor did not resolve a current state.");

        return context.StateMachine.NextEvents(state);
    }
}
