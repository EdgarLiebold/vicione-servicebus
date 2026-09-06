using System;
using System.Linq.Expressions;
using FastExpressionCompiler;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Converts saga filter expression values.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class SagaFilterExpressionConverter<TSaga, TMessage> :
    ExpressionVisitor
{
    readonly TMessage _message;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public SagaFilterExpressionConverter(TMessage message)
    {
        _message = message;
    }

    /// <summary>Converts the supplied value.</summary>
    /// <param name="expression">The expression.</param>
    /// <returns>The converted value.</returns>
    public Expression<Func<TSaga, bool>> Convert(Expression<Func<TSaga, TMessage, bool>> expression)
    {
        var result = Visit(expression) as LambdaExpression
            ?? throw new InvalidOperationException("The saga filter expression could not be converted to a lambda expression.");

        return RemoveMessageParameter(result);
    }

    /// <summary>Visits member.</summary>
    /// <param name="m">The <c>m</c> value.</param>
    /// <returns>The expression produced by the operation.</returns>
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
