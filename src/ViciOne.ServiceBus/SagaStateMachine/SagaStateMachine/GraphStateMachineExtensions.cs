// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SagaStateMachine
{
    public static class GraphStateMachineExtensions
    {
        public static StateMachineGraph GetGraph<TSaga>(this StateMachine<TSaga> machine)
            where TSaga : class, SagaStateMachineInstance
        {
            var inspector = new GraphStateMachineVisitor<TSaga>(machine);

            machine.Accept(inspector);

            return inspector.Graph;
        }
    }
}
