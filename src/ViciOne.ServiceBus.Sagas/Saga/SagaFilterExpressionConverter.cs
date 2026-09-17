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
    readonly object _conversionLock = new();
    readonly TMessage _message;
    ParameterExpression? _messageParameter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public SagaFilterExpressionConverter(TMessage message)
    {
        _message = message ?? throw new ArgumentNullException(nameof(message));
    }

    /// <summary>Converts the supplied value.</summary>
    /// <param name="expression">The expression.</param>
    /// <returns>The converted value.</returns>
    public Expression<Func<TSaga, bool>> Convert(Expression<Func<TSaga, TMessage, bool>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        lock (_conversionLock)
        {
            _messageParameter = expression.Parameters[1];
            try
            {
                Expression convertedBody = Visit(expression.Body)
                    ?? throw new InvalidOperationException("The saga filter expression could not be converted to a lambda expression.");
                LambdaExpression result = Expression.Lambda(convertedBody, expression.Parameters);

                return RemoveMessageParameter(result);
            }
            finally
            {
                _messageParameter = null;
            }
        }
    }

    /// <summary>Visits member.</summary>
    /// <param name="m">The <c>m</c> value.</param>
    /// <returns>The expression produced by the operation.</returns>
    protected override Expression VisitMember(MemberExpression m)
    {
        if (ReferenceEquals(m.Expression, _messageParameter))
            return EvaluateMemberAccess(m);

        return base.VisitMember(m);
    }

    /// <summary>Replaces the message parameter with the message supplied to this converter.</summary>
    /// <param name="node">The parameter being visited.</param>
    /// <returns>The replacement constant for the message parameter, or the base visitor result.</returns>
    protected override Expression VisitParameter(ParameterExpression node)
    {
        return ReferenceEquals(node, _messageParameter)
            ? Expression.Constant(_message, typeof(TMessage))
            : base.VisitParameter(node);
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
