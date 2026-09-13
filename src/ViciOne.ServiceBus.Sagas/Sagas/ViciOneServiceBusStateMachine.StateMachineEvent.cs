namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    class StateMachineEvent
    {
        public StateMachineEvent(IEvent @event, bool isTransitionEvent)
        {
            Event = @event;
            IsTransitionEvent = isTransitionEvent;
        }

        public bool IsTransitionEvent { get; }
        public IEvent Event { get; }
    }
}
