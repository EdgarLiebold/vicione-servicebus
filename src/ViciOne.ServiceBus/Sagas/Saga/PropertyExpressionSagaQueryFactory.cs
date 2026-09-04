using System;
using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Saga;

public class PropertyExpressionSagaQueryFactory<TInstance, TData, TProperty> :
    ISagaQueryFactory<TInstance, TData>
    where TInstance : class, SagaStateMachineInstance
    where TData : class
{
    readonly Expression<Func<TInstance, TProperty>> _propertyExpression;
    readonly PropertyInfo _propertyInfo;
    readonly ISagaQueryPropertySelector<TData, TProperty> _selector;

    public PropertyExpressionSagaQueryFactory(Expression<Func<TInstance, TProperty>> propertyExpression,
        ISagaQueryPropertySelector<TData, TProperty> selector)
    {
        _propertyExpression = propertyExpression;
        _selector = selector;

        _propertyInfo = typeof(PropertyExpressionPropertyValue<TProperty>).GetProperty(nameof(PropertyExpressionPropertyValue<TProperty>.Value))
            ?? throw new InvalidOperationException("The saga query value property was not found.");
    }

    public bool TryCreateQuery(ConsumeContext<TData> context, [NotNullWhen(true)] out ISagaQuery<TInstance>? query)
    {
        if (_selector.TryGetProperty(context, out var propertyValue))
        {
            Expression<Func<TInstance, bool>> filterExpression = CreateExpression(propertyValue);

            query = new SagaQuery<TInstance>(filterExpression);
            return true;
        }

        query = null;
        return false;
    }

    public void Probe(ProbeContext context)
    {
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
