using System;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides extension methods for saga query expression property.</summary>
public static class SagaQueryExpressionPropertyExtensions
{
    /// <summary>Analyzes the saga query and determines if it contains a binary expression that queries by a property value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="query">The query.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentException">Thrown when an argument does not satisfy the operation contract.</exception>
    public static bool TryGetPropertyValue<T>(this ISagaQuery<T> query, out object? value)
        where T : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(query);

        Expression<Func<T, bool>> expression = query.FilterExpression;
        if (expression == null)
            throw new ArgumentException("The query is not a lambda expression", nameof(query));

        if (expression.Body is BinaryExpression
            {
                Right: MemberExpression
                {
                    Member.Name: "Value", Expression: ConstantExpression { Value: IPropertyExpressionPropertyValue propertyValue }
                }
            })
        {
            value = propertyValue.GetValue();
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>Analyzes the saga query and determines if it contains a binary expression that queries by a property value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="query">The query.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentException">Thrown when an argument does not satisfy the operation contract.</exception>
    public static bool TryGetPropertyValue<T, TProperty>(this ISagaQuery<T> query, out TProperty? value)
        where T : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(query);

        Expression<Func<T, bool>> expression = query.FilterExpression;
        if (expression == null)
            throw new ArgumentException("The query is not a lambda expression", nameof(query));

        if (expression.Body is BinaryExpression
            {
                Right: MemberExpression
                {
                    Member.Name: "Value", Expression: ConstantExpression { Value: PropertyExpressionPropertyValue<TProperty> propertyValue }
                }
            })
        {
            value = propertyValue.Value;
            return true;
        }

        value = default;
        return false;
    }
}
