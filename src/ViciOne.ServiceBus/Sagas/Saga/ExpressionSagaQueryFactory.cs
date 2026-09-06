using System;
using System.Linq.Expressions;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Creates a saga query using the specified filter expression.</summary>
/// <typeparam name="TSaga">The saga type.</typeparam>
/// <typeparam name="TMessage">The message type.</typeparam>
public class ExpressionSagaQueryFactory<TSaga, TMessage> :
    ISagaQueryFactory<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly Expression<Func<TSaga, TMessage, bool>> _filterExpression;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="filterExpression">The filter expression.</param>
    public ExpressionSagaQueryFactory(Expression<Func<TSaga, TMessage, bool>> filterExpression)
    {
        _filterExpression = filterExpression;
    }

    bool ISagaQueryFactory<TSaga, TMessage>.TryCreateQuery(ConsumeContext<TMessage> context, out ISagaQuery<TSaga> query)
    {
        Expression<Func<TSaga, bool>> expression = new SagaFilterExpressionConverter<TSaga, TMessage>(context.Message)
            .Convert(_filterExpression);

        query = new SagaQuery<TSaga>(expression);
        return true;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.Add("expression", _filterExpression.ToString());
    }
}
