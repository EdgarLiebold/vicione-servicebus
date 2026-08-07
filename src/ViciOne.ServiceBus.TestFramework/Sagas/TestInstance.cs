// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.Sagas
{
    using System;


    public class TestInstance :
        SagaStateMachineInstance
    {
        public State CurrentState { get; set; }

        public string Key { get; set; }
        public Guid CorrelationId { get; set; }
    }
}
