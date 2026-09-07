namespace ViciOne.ServiceBus.Sagas;

/// <summary>Identifies a saga instance whose state transitions are owned by a ViciOne ServiceBus state machine.</summary>
public interface SagaStateMachineInstance :
    ISaga
{
}
