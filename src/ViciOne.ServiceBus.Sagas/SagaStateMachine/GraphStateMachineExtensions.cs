namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Provides extension methods for graph state machine.</summary>
public static class GraphStateMachineExtensions
{
    /// <summary>Gets graph.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="machine">The machine.</param>
    /// <returns>The graph.</returns>
    public static StateMachineGraph GetGraph<TSaga>(this StateMachine<TSaga> machine)
        where TSaga : class, SagaStateMachineInstance
    {
        var inspector = new GraphStateMachineVisitor<TSaga>(machine);

        machine.Accept(inspector);

        return inspector.Graph;
    }
}
