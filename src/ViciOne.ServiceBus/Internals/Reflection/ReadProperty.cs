using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using FastExpressionCompiler;

namespace ViciOne.ServiceBus.Internals.Reflection;

internal sealed class ReadProperty<T, TProperty> : IReadProperty<T, TProperty>
    where T : class
{
    readonly Func<T, TProperty> _getMethod;

    public ReadProperty(PropertyInfo propertyInfo)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);

        if (propertyInfo.DeclaringType == null || !propertyInfo.DeclaringType.IsAssignableFrom(typeof(T)))
            throw new ArgumentException($"Property {propertyInfo.Name} cannot be read from {typeof(T)}.", nameof(propertyInfo));

        if (propertyInfo.GetIndexParameters().Length != 0)
            throw new ArgumentException($"Indexed property {propertyInfo.Name} is not supported.", nameof(propertyInfo));

        var getMethod = propertyInfo.GetGetMethod(true)
            ?? throw new ArgumentException($"The property does not have a getter: {propertyInfo.Name}", nameof(propertyInfo));

        if (getMethod.IsStatic)
            throw new ArgumentException($"Static property {propertyInfo.Name} is not supported.", nameof(propertyInfo));

        if (propertyInfo.PropertyType != typeof(TProperty))
            throw new ArgumentException($"Property type {propertyInfo.PropertyType} does not match {typeof(TProperty)}.", nameof(propertyInfo));

        _getMethod = CreateGetter(getMethod);
    }

    public TProperty Get(T content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return _getMethod(content);
    }

    static Func<T, TProperty> CreateGetter(MethodInfo getMethod)
    {
        if (!RuntimeFeature.IsDynamicCodeSupported || !getMethod.IsPublic)
            return entity => InvokeGetter(getMethod, entity);

        try
        {
            var instance = Expression.Parameter(typeof(T), "instance");
            Expression target = getMethod.DeclaringType == typeof(T)
                ? instance
                : Expression.Convert(instance, getMethod.DeclaringType!);
            var call = Expression.Call(target, getMethod);
            return Expression.Lambda<Func<T, TProperty>>(call, instance).CompileFast<Func<T, TProperty>>();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or MemberAccessException or NotSupportedException)
        {
            return entity => InvokeGetter(getMethod, entity);
        }
    }

    static TProperty InvokeGetter(MethodInfo getMethod, T entity)
    {
        try
        {
            return (TProperty)getMethod.Invoke(entity, null)!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}
