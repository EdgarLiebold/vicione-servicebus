namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Provides a vici one service bus state machine implementation.
/// </summary>
public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    class StateMachineEvent
    {
        public StateMachineEvent(Event @event, bool isTransitionEvent)
        {
            Event = @event;
            IsTransitionEvent = isTransitionEvent;
        }

        public bool IsTransitionEvent { get; }
        public Event Event { get; }
    }
}
