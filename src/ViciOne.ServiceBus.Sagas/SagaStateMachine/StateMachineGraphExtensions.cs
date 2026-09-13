using System;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Provides graph inspection for configured saga state machines.</summary>
public static class StateMachineGraphExtensions
{
    /// <summary>Creates an immutable graph snapshot of the configured state-machine relationships.</summary>
    /// <typeparam name="TSaga">The saga instance managed by the state machine.</typeparam>
    /// <param name="machine">The configured state machine to inspect.</param>
    /// <returns>
    /// A graph snapshot containing states, state-local event bindings, transitions, exception branches, composite-event
    /// contributions, state inheritance, and disconnected declarations.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="machine" /> is <see langword="null" />.</exception>
    public static StateMachineGraph GetGraph<TSaga>(this IStateMachine<TSaga> machine)
        where TSaga : class, ISagaStateMachineInstance
    {
        ArgumentNullException.ThrowIfNull(machine);

        var visitor = new StateMachineGraphVisitor<TSaga>(machine);
        machine.Accept(visitor);
        return visitor.Graph;
    }
}
