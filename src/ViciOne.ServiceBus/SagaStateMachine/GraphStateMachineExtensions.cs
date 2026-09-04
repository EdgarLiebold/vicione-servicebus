namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides extension methods for graph state machine.
/// </summary>
public static class GraphStateMachineExtensions
{
    /// <summary>
    /// Gets graph.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="machine">The machine value.</param>
    /// <returns>The result of the operation.</returns>
    public static StateMachineGraph GetGraph<TSaga>(this StateMachine<TSaga> machine)
        where TSaga : class, SagaStateMachineInstance
    {
        var inspector = new GraphStateMachineVisitor<TSaga>(machine);

        machine.Accept(inspector);

        return inspector.Graph;
    }
}
