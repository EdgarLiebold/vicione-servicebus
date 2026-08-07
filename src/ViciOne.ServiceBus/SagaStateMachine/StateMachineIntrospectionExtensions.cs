// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.Collections.Generic;
    using System.Threading.Tasks;


    public static class StateMachineIntrospectionExtensions
    {
        public static async Task<IEnumerable<Event>> NextEvents<TInstance>(this BehaviorContext<TInstance> context)
            where TInstance : class, SagaStateMachineInstance
        {
            return context.StateMachine.NextEvents(await context.StateMachine.Accessor.Get(context));
        }
    }
}
