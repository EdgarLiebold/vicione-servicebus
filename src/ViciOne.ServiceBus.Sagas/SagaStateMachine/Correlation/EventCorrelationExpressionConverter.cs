using System;
using System.Linq.Expressions;
using FastExpressionCompiler;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Converts event correlation expression values.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class EventCorrelationExpressionConverter<TInstance, TMessage> :
    ExpressionVisitor
    where TInstance : class, ISagaStateMachineInstance
    where TMessage : class
{
    readonly object _conversionLock = new();
    readonly ConsumeContext<TMessage> _context;
    ParameterExpression? _contextParameter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public EventCorrelationExpressionConverter(ConsumeContext<TMessage> context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>Converts the supplied value.</summary>
    /// <param name="expression">The expression.</param>
    /// <returns>The converted value.</returns>
    public Expression<Func<TInstance, bool>> Convert(Expression<Func<TInstance, ConsumeContext<TMessage>, bool>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        lock (_conversionLock)
        {
            ParameterExpression? previousContextParameter = _contextParameter;
            _contextParameter = expression.Parameters[1];
            try
            {
                var result = Visit(expression) as LambdaExpression
                    ?? throw new InvalidOperationException("The correlation expression could not be converted to a lambda expression.");

                return RemoveMessageParameter(result);
            }
            finally
            {
                _contextParameter = previousContextParameter;
            }
        }
    }

    static Expression<Func<TInstance, bool>> RemoveMessageParameter(LambdaExpression lambda)
    {
        ParameterExpression[] parameters = { lambda.Parameters[0] };

        return Expression.Lambda<Func<TInstance, bool>>(lambda.Body, parameters);
    }

    /// <summary>Visits member.</summary>
    /// <param name="m">The <c>m</c> value.</param>
    /// <returns>The expression produced by the operation.</returns>
    protected override Expression VisitMember(MemberExpression m)
    {
        if (m.Expression == null)
            return base.VisitMember(m);

        ParameterExpression? contextParameter = _contextParameter;
        if (contextParameter != null && IsRootedInParameter(m, contextParameter))
            return EvaluateConsumeContextAccess(m, contextParameter);

        return base.VisitMember(m);
    }

    static bool IsRootedInParameter(MemberExpression expression, ParameterExpression parameter)
    {
        Expression? current = expression;
        while (current is MemberExpression member)
            current = member.Expression;

        return ReferenceEquals(current, parameter);
    }

    Expression EvaluateConsumeContextAccess(MemberExpression expression, ParameterExpression parameter)
    {
        var valueExpression = Expression.Convert(expression, typeof(object));
        Func<ConsumeContext<TMessage>, object?> evaluator =
            Expression.Lambda<Func<ConsumeContext<TMessage>, object?>>(valueExpression, parameter).CompileFast();
        object? value = evaluator(_context);

        if (value == null)
            return Expression.Constant(value, expression.Type);

        Type? nullableType = Nullable.GetUnderlyingType(expression.Type);
        if (nullableType != null && nullableType.IsInstanceOfType(value))
            return Expression.Convert(Expression.Constant(value, nullableType), expression.Type);

        return Expression.Constant(value, expression.Type);
    }
}
