using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public static class StateMachineIntrospectionExtensions
{
    public static async Task<IEnumerable<Event>> NextEventsAsync<TInstance>(this BehaviorContext<TInstance> context, CancellationToken cancellationToken = default)
        where TInstance : class, SagaStateMachineInstance
    {
        var state = await context.StateMachine.Accessor.GetAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The state machine accessor did not resolve a current state.");

        return context.StateMachine.NextEvents(state);
    }
}
