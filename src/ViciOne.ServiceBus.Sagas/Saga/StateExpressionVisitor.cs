using System;
using System.Linq.Expressions;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Visits state expression graph elements.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
public class StateExpressionVisitor<TInstance> :
    ExpressionVisitor
{
    readonly Expression _expressionBody;
    readonly ParameterExpression _instanceParameter;
    ParameterExpression? _stateParameter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="expression">The expression.</param>
    public StateExpressionVisitor(Expression<Func<TInstance, bool>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        _instanceParameter = expression.Parameters[0];
        _expressionBody = expression.Body;
    }

    /// <summary>Combines the base expression with the specified state expression (from the state accessor).</summary>
    /// <param name="stateExpression">The state expression.</param>
    /// <param name="not">If true, adds a not to the expression, otherwise, matches any of the states.</param>
    /// <returns>The combined expression.</returns>
    Expression<Func<TInstance, bool>> Combine(Expression<Func<TInstance, bool>> stateExpression, bool not = false)
    {
        ArgumentNullException.ThrowIfNull(stateExpression);
        _stateParameter = stateExpression.Parameters[0];
        var result = Visit(stateExpression);
        if (result is LambdaExpression lambda)
        {
            var stateExpressionBody = not
                ? Expression.Not(lambda.Body)
                : lambda.Body;

            return Expression.Lambda<Func<TInstance, bool>>(Expression.AndAlso(_expressionBody, stateExpressionBody), _instanceParameter);
        }

        throw new ArgumentException("Could not combine the expression", nameof(stateExpression));
    }

    /// <summary>Visits parameter.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The expression produced by the operation.</returns>
    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (ReferenceEquals(node, _stateParameter))
            return _instanceParameter;

        return base.VisitParameter(node);
    }

    /// <summary>Combines the supplied values.</summary>
    /// <param name="expression">The expression.</param>
    /// <param name="stateExpression">The state expression.</param>
    /// <returns>The expression produced by the operation.</returns>
    public static Expression<Func<TInstance, bool>> Combine(Expression<Func<TInstance, bool>> expression, Expression<Func<TInstance, bool>> stateExpression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(stateExpression);
        return new StateExpressionVisitor<TInstance>(expression).Combine(stateExpression);
    }
}
