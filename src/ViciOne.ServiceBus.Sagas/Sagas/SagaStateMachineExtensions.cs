using System;
using System.Linq.Expressions;
using FastExpressionCompiler;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides extension methods for saga state machine.</summary>
public static class SagaStateMachineExtensions
{
    /// <summary>Create a query that combines the specified expression with an expression that compares the instance state with the specified states.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <param name="machine">The state machine.</param>
    /// <param name="expression">The query expression.</param>
    /// <param name="states">The states that are valid for this query.</param>
    /// <returns>The created saga query.</returns>
    public static ISagaQuery<TInstance> CreateSagaQuery<TInstance>(this IStateMachine<TInstance> machine, Expression<Func<TInstance, bool>> expression,
        params IState[] states)
        where TInstance : class, ISagaStateMachineInstance
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(expression);
        ValidateStates(states);

        Expression<Func<TInstance, bool>> stateExpression = machine.Accessor.GetStateExpression(states);

        return new SagaQuery<TInstance>(StateExpressionVisitor<TInstance>.Combine(expression, stateExpression));
    }

    /// <summary>Create a query that combines the specified expression with an expression that compares the instance state with the specified states.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <param name="machine">The state machine.</param>
    /// <param name="expression">The query expression.</param>
    /// <param name="states">The states that are valid for this query.</param>
    /// <returns>The created saga filter.</returns>
    public static Func<TInstance, bool> CreateSagaFilter<TInstance>(this IStateMachine<TInstance> machine, Expression<Func<TInstance, bool>> expression,
        params IState[] states)
        where TInstance : class, ISagaStateMachineInstance
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(expression);
        ValidateStates(states);

        Expression<Func<TInstance, bool>> stateExpression = machine.Accessor.GetStateExpression(states);

        return StateExpressionVisitor<TInstance>.Combine(expression, stateExpression).CompileFast();
    }

    static void ValidateStates(IState[] states)
    {
        ArgumentNullException.ThrowIfNull(states);
        if (Array.Exists(states, static state => state is null))
            throw new ArgumentException("States must not contain null values.", nameof(states));
    }
}
