using System;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// Provides a saga filter expression converter implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class SagaFilterExpressionConverter<TSaga, TMessage> :
    ExpressionVisitor
{
    readonly TMessage _message;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public SagaFilterExpressionConverter(TMessage message)
    {
        _message = message;
    }

    /// <summary>
    /// Performs the convert operation.
    /// </summary>
    /// <param name="expression">The expression value.</param>
    /// <returns>The result of the operation.</returns>
    public Expression<Func<TSaga, bool>> Convert(Expression<Func<TSaga, TMessage, bool>> expression)
    {
        var result = Visit(expression) as LambdaExpression
            ?? throw new InvalidOperationException("The saga filter expression could not be converted to a lambda expression.");

        return RemoveMessageParameter(result);
    }

    /// <summary>
    /// Performs the visit member operation.
    /// </summary>
    /// <param name="m">The m value.</param>
    /// <returns>The result of the operation.</returns>
    protected override Expression VisitMember(MemberExpression m)
    {
        if (m.Expression != null && m.Expression.NodeType == ExpressionType.Parameter && m.Expression.Type == typeof(TMessage))
            return EvaluateMemberAccess(m);

        return base.VisitMember(m);
    }

    static Expression<Func<TSaga, bool>> RemoveMessageParameter(LambdaExpression lambda)
    {
        ParameterExpression[] parameters = { lambda.Parameters[0] };

        return Expression.Lambda<Func<TSaga, bool>>(lambda.Body, parameters);
    }

    Expression EvaluateMemberAccess(MemberExpression exp)
    {
        var parameter = exp.Expression as ParameterExpression
            ?? throw new InvalidOperationException("The message access must originate from a parameter expression.");

        var fn = Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(TMessage), exp.Type), exp, parameter).CompileFast();

        return Expression.Constant(fn.DynamicInvoke(_message), exp.Type);
    }
}
