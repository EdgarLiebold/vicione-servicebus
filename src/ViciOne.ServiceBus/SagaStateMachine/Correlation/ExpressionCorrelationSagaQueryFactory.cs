using System;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides an expression correlation saga query factory implementation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TData">The t data type.</typeparam>
public class ExpressionCorrelationSagaQueryFactory<TInstance, TData> :
    ISagaQueryFactory<TInstance, TData>
    where TInstance : class, SagaStateMachineInstance
    where TData : class
{
    readonly Expression<Func<TInstance, ConsumeContext<TData>, bool>> _correlationExpression;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="correlationExpression">The correlation expression value.</param>
    public ExpressionCorrelationSagaQueryFactory(Expression<Func<TInstance, ConsumeContext<TData>, bool>> correlationExpression)
    {
        _correlationExpression = correlationExpression;
    }

    /// <summary>
    /// Performs the try create query operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="query">The query value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryCreateQuery(ConsumeContext<TData> context, out ISagaQuery<TInstance> query)
    {
        Expression<Func<TInstance, bool>> filter = new EventCorrelationExpressionConverter<TInstance, TData>(context)
            .Convert(_correlationExpression);

        query = new SagaQuery<TInstance>(filter);
        return true;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.Add("expression", _correlationExpression.ToString());
    }
}
