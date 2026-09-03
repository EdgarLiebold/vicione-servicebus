namespace ViciOne.ServiceBus.Metadata;

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;


internal static class PropertyAccessorFactory
{
    public static PropertyInfo Validate(PropertyInfo? property, Type? targetType = null, Type? propertyType = null)
    {
        ArgumentNullException.ThrowIfNull(property);

        Type declaringType = property.DeclaringType
            ?? throw new ArgumentException("The property must have a declaring type.", nameof(property));

        if (property.GetIndexParameters().Length != 0)
            throw new ArgumentException($"Indexed property {declaringType.Name}.{property.Name} is not supported.", nameof(property));

        MethodInfo? accessor = property.GetMethod ?? property.SetMethod;
        if (accessor?.IsStatic == true)
            throw new ArgumentException($"Static property {declaringType.Name}.{property.Name} is not supported.", nameof(property));

        if (targetType != null && !declaringType.IsAssignableFrom(targetType))
            throw new ArgumentException($"Property {declaringType.Name}.{property.Name} cannot be used with target type {targetType}.", nameof(property));

        if (propertyType != null && property.PropertyType != propertyType)
            throw new ArgumentException($"Property type {property.PropertyType} does not match {propertyType}.", nameof(property));

        return property;
    }

    public static bool IncludeNonPublic(PropertyAccessPolicy accessPolicy)
    {
        return accessPolicy switch
        {
            PropertyAccessPolicy.PublicOnly => false,
            PropertyAccessPolicy.IncludeNonPublic => true,
            _ => throw new ArgumentOutOfRangeException(nameof(accessPolicy), accessPolicy, "Unknown property access policy.")
        };
    }

    public static Func<object, object?> CreateUntypedGetter(PropertyInfo property, PropertyAccessPolicy accessPolicy)
    {
        MethodInfo? getMethod = property.GetGetMethod(IncludeNonPublic(accessPolicy));
        if (getMethod == null)
            return _ => throw MissingAccessor(property, "getter");

        if (!CanCompile(getMethod))
            return instance => InvokeGetter(getMethod, instance);

        try
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var target = Expression.Convert(instance, getMethod.DeclaringType!);
            var call = Expression.Call(target, getMethod);
            return Expression.Lambda<Func<object, object?>>(Expression.Convert(call, typeof(object)), instance).Compile();
        }
        catch (Exception exception) when (IsCompilationFailure(exception))
        {
            return instance => InvokeGetter(getMethod, instance);
        }
    }

    public static Func<T, object?> CreateGetter<T>(PropertyInfo property, PropertyAccessPolicy accessPolicy)
    {
        MethodInfo? getMethod = property.GetGetMethod(IncludeNonPublic(accessPolicy));
        if (getMethod == null)
            return _ => throw MissingAccessor(property, "getter");

        if (!CanCompile(getMethod))
            return instance => InvokeGetter(getMethod, instance);

        try
        {
            var instance = Expression.Parameter(typeof(T), "instance");
            Expression target = getMethod.DeclaringType == typeof(T)
                ? instance
                : Expression.Convert(instance, getMethod.DeclaringType!);
            var call = Expression.Call(target, getMethod);
            return Expression.Lambda<Func<T, object?>>(Expression.Convert(call, typeof(object)), instance).Compile();
        }
        catch (Exception exception) when (IsCompilationFailure(exception))
        {
            return instance => InvokeGetter(getMethod, instance);
        }
    }

    public static Func<T, TProperty> CreateGetter<T, TProperty>(PropertyInfo property, PropertyAccessPolicy accessPolicy)
    {
        MethodInfo? getMethod = property.GetGetMethod(IncludeNonPublic(accessPolicy));
        if (getMethod == null)
            return _ => throw MissingAccessor(property, "getter");

        if (!CanCompile(getMethod))
            return instance => (TProperty)InvokeGetter(getMethod, instance)!;

        try
        {
            var instance = Expression.Parameter(typeof(T), "instance");
            Expression target = getMethod.DeclaringType == typeof(T)
                ? instance
                : Expression.Convert(instance, getMethod.DeclaringType!);
            var call = Expression.Call(target, getMethod);
            return Expression.Lambda<Func<T, TProperty>>(call, instance).Compile();
        }
        catch (Exception exception) when (IsCompilationFailure(exception))
        {
            return instance => (TProperty)InvokeGetter(getMethod, instance)!;
        }
    }

    public static Action<object, object?> CreateUntypedSetter(PropertyInfo property, PropertyAccessPolicy accessPolicy)
    {
        MethodInfo? setMethod = property.GetSetMethod(IncludeNonPublic(accessPolicy));
        if (setMethod == null)
            return (_, _) => throw MissingAccessor(property, "setter");

        Action<object, object?> setter;
        if (!CanCompile(setMethod))
            setter = (instance, value) => InvokeSetter(setMethod, instance, value);
        else
        {
            try
            {
                var instance = Expression.Parameter(typeof(object), "instance");
                var value = Expression.Parameter(typeof(object), "value");
                var target = Expression.Convert(instance, setMethod.DeclaringType!);
                var call = Expression.Call(target, setMethod, Expression.Convert(value, property.PropertyType));
                setter = Expression.Lambda<Action<object, object?>>(call, instance, value).Compile();
            }
            catch (Exception exception) when (IsCompilationFailure(exception))
            {
                setter = (instance, value) => InvokeSetter(setMethod, instance, value);
            }
        }

        return (instance, value) =>
        {
            ValidateSetterValue(property, value);
            setter(instance, value);
        };
    }

    public static Action<T, object?> CreateSetter<T>(PropertyInfo property, PropertyAccessPolicy accessPolicy)
    {
        MethodInfo? setMethod = property.GetSetMethod(IncludeNonPublic(accessPolicy));
        if (setMethod == null)
            return (_, _) => throw MissingAccessor(property, "setter");

        Action<T, object?> setter;
        if (!CanCompile(setMethod))
            setter = (instance, value) => InvokeSetter(setMethod, instance, value);
        else
        {
            try
            {
                var instance = Expression.Parameter(typeof(T), "instance");
                var value = Expression.Parameter(typeof(object), "value");
                Expression target = setMethod.DeclaringType == typeof(T)
                    ? instance
                    : Expression.Convert(instance, setMethod.DeclaringType!);
                var call = Expression.Call(target, setMethod, Expression.Convert(value, property.PropertyType));
                setter = Expression.Lambda<Action<T, object?>>(call, instance, value).Compile();
            }
            catch (Exception exception) when (IsCompilationFailure(exception))
            {
                setter = (instance, value) => InvokeSetter(setMethod, instance, value);
            }
        }

        return (instance, value) =>
        {
            ValidateSetterValue(property, value);
            setter(instance, value);
        };
    }

    public static Action<T, TProperty> CreateSetter<T, TProperty>(PropertyInfo property, PropertyAccessPolicy accessPolicy)
    {
        MethodInfo? setMethod = property.GetSetMethod(IncludeNonPublic(accessPolicy));
        if (setMethod == null)
            return (_, _) => throw MissingAccessor(property, "setter");

        if (!CanCompile(setMethod))
            return (instance, value) => InvokeSetter(setMethod, instance, value);

        try
        {
            var instance = Expression.Parameter(typeof(T), "instance");
            var value = Expression.Parameter(typeof(TProperty), "value");
            Expression target = setMethod.DeclaringType == typeof(T)
                ? instance
                : Expression.Convert(instance, setMethod.DeclaringType!);
            var call = Expression.Call(target, setMethod, value);
            return Expression.Lambda<Action<T, TProperty>>(call, instance, value).Compile();
        }
        catch (Exception exception) when (IsCompilationFailure(exception))
        {
            return (instance, value) => InvokeSetter(setMethod, instance, value);
        }
    }

    static bool CanCompile(MethodInfo accessor)
    {
        return RuntimeFeature.IsDynamicCodeSupported && accessor.IsPublic && accessor.DeclaringType?.IsValueType == false;
    }

    static bool IsCompilationFailure(Exception exception)
    {
        return exception is ArgumentException or InvalidOperationException or MemberAccessException or NotSupportedException;
    }

    static void ValidateSetterValue(PropertyInfo property, object? value)
    {
        Type? nullableType = Nullable.GetUnderlyingType(property.PropertyType);

        if (value == null)
        {
            if (property.PropertyType.IsValueType && nullableType == null)
                throw new ArgumentNullException(nameof(value), $"Property {property.DeclaringType?.Name}.{property.Name} does not accept null.");

            return;
        }

        Type acceptedType = nullableType ?? property.PropertyType;
        if (!acceptedType.IsInstanceOfType(value))
        {
            throw new ArgumentException(
                $"Value type {value.GetType()} cannot be assigned to property {property.DeclaringType?.Name}.{property.Name} of type {property.PropertyType}.",
                nameof(value));
        }
    }

    static object? InvokeGetter(MethodInfo getMethod, object? instance)
    {
        try
        {
            return getMethod.Invoke(instance, null);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    static void InvokeSetter(MethodInfo setMethod, object? instance, object? value)
    {
        try
        {
            setMethod.Invoke(instance, [value]);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
        }
    }

    static InvalidOperationException MissingAccessor(PropertyInfo property, string accessor)
    {
        return new InvalidOperationException($"No eligible {accessor} is available for {property.DeclaringType?.Name}.{property.Name}.");
    }
}
