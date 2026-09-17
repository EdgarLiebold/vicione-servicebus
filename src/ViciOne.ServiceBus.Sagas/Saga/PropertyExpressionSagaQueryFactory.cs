using System;
using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Creates property expression saga query instances.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public class PropertyExpressionSagaQueryFactory<TInstance, TData, TProperty> :
    ISagaQueryFactory<TInstance, TData>
    where TInstance : class, ISagaStateMachineInstance
    where TData : class
{
    readonly Expression<Func<TInstance, TProperty>> _propertyExpression;
    readonly PropertyInfo _propertyInfo;
    readonly ISagaQueryPropertySelector<TData, TProperty> _selector;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="propertyExpression">The property expression.</param>
    /// <param name="selector">The selector.</param>
    public PropertyExpressionSagaQueryFactory(Expression<Func<TInstance, TProperty>> propertyExpression,
        ISagaQueryPropertySelector<TData, TProperty> selector)
    {
        _propertyExpression = propertyExpression ?? throw new ArgumentNullException(nameof(propertyExpression));
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));

        _propertyInfo = typeof(PropertyExpressionPropertyValue<TProperty>).GetProperty(nameof(PropertyExpressionPropertyValue<TProperty>.Value))
            ?? throw new InvalidOperationException("The saga query value property was not found.");
    }

    /// <summary>Attempts to create query.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="query">Receives the query produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryCreateQuery(ConsumeContext<TData> context, [NotNullWhen(true)] out ISagaQuery<TInstance>? query)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_selector.TryGetProperty(context, out var propertyValue))
        {
            Expression<Func<TInstance, bool>> filterExpression = CreateExpression(propertyValue);

            query = new SagaQuery<TInstance>(filterExpression);
            return true;
        }

        query = null;
        return false;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Add("property", _propertyExpression.ToString());
    }

    Expression<Func<TInstance, bool>> CreateExpression(TProperty propertyValue)
    {
        var value = new PropertyExpressionPropertyValue<TProperty> { Value = propertyValue };

        var constantValue = Expression.Constant(value);
        var memberAccess = Expression.MakeMemberAccess(constantValue, _propertyInfo);

        return Expression.Lambda<Func<TInstance, bool>>(Expression.Equal(_propertyExpression.Body, memberAccess), _propertyExpression.Parameters);
    }
}
