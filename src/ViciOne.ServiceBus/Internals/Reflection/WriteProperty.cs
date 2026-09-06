using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using FastExpressionCompiler;

namespace ViciOne.ServiceBus.Internals;

internal class WriteProperty<T, TProperty> : IWriteProperty<T, TProperty>
    where T : class
{
    readonly Action<T, TProperty> _setMethod;

    public WriteProperty(Type implementationType, PropertyInfo propertyInfo)
    {
        ArgumentNullException.ThrowIfNull(implementationType);
        ArgumentNullException.ThrowIfNull(propertyInfo);

        if (!typeof(T).IsAssignableFrom(implementationType))
            throw new ArgumentException($"Implementation type {implementationType} is not assignable to {typeof(T)}.", nameof(implementationType));

        if (propertyInfo.DeclaringType == null || !propertyInfo.DeclaringType.IsAssignableFrom(implementationType))
            throw new ArgumentException($"Property {propertyInfo.Name} cannot be used with implementation type {implementationType}.", nameof(propertyInfo));

        if (propertyInfo.PropertyType != typeof(TProperty))
            throw new ArgumentException($"Property type {propertyInfo.PropertyType} does not match {typeof(TProperty)}.", nameof(propertyInfo));

        TargetType = implementationType;

        var setMethod = propertyInfo.GetSetMethod(true)
            ?? throw new ArgumentException($"The property does not have a setter: {propertyInfo.Name}", nameof(propertyInfo));

        _setMethod = CreateSetter(implementationType, setMethod);
    }

    public Type TargetType { get; }

    public void Set(T content, TProperty? value) => _setMethod(content, value!);

    static Action<T, TProperty> CreateSetter(Type implementationType, MethodInfo setMethod)
    {
        if (!RuntimeFeature.IsDynamicCodeSupported || !setMethod.IsPublic)
            return (entity, value) => InvokeSetter(setMethod, entity, value);

        try
        {
            var instance = Expression.Parameter(typeof(T), "instance");
            var value = Expression.Parameter(typeof(TProperty), "value");
            var target = Expression.Convert(instance, implementationType);
            var call = Expression.Call(target, setMethod, value);
            return Expression.Lambda<Action<T, TProperty>>(call, instance, value).CompileFast<Action<T, TProperty>>();
        }
        catch (Exception exception) when (IsCompilationFailure(exception))
        {
            return (entity, value) => InvokeSetter(setMethod, entity, value);
        }
    }

    static bool IsCompilationFailure(Exception exception) =>
        exception is ArgumentException or InvalidOperationException or MemberAccessException or NotSupportedException;

    static void InvokeSetter(MethodInfo setMethod, T entity, TProperty value)
    {
        try
        {
            setMethod.Invoke(entity, [value]);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
        }
    }
}
