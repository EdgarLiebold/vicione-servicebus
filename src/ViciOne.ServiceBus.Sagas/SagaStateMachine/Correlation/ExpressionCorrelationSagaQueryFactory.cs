using System;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Creates expression correlation saga query instances.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
public class ExpressionCorrelationSagaQueryFactory<TInstance, TData> :
    ISagaQueryFactory<TInstance, TData>
    where TInstance : class, ISagaStateMachineInstance
    where TData : class
{
    readonly Expression<Func<TInstance, ConsumeContext<TData>, bool>> _correlationExpression;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="correlationExpression">The correlation expression.</param>
    public ExpressionCorrelationSagaQueryFactory(Expression<Func<TInstance, ConsumeContext<TData>, bool>> correlationExpression)
    {
        _correlationExpression = correlationExpression;
    }

    /// <summary>Attempts to create query.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="query">Receives the query produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryCreateQuery(ConsumeContext<TData> context, out ISagaQuery<TInstance> query)
    {
        Expression<Func<TInstance, bool>> filter = new EventCorrelationExpressionConverter<TInstance, TData>(context)
            .Convert(_correlationExpression);

        query = new SagaQuery<TInstance>(filter);
        return true;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.Add("expression", _correlationExpression.ToString());
    }
}
