using System;
using System.Runtime.CompilerServices;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>
/// Invokes generic activation logic for a type that is known only at run time.
/// </summary>
internal static class Activation
{
    static readonly ConditionalWeakTable<Type, CachedType> CachedTypes = new();

    /// <summary>
    /// Invokes the supplied activation strategy with the run-time type as its generic type argument.
    /// </summary>
    /// <typeparam name="TResult">The value returned by the activation strategy.</typeparam>
    /// <param name="type">The closed reference type to activate.</param>
    /// <param name="activationType">The strategy that performs the type-specific operation.</param>
    /// <returns>The value returned by <paramref name="activationType"/>.</returns>
    public static TResult Activate<TResult>(Type type, IActivationType<TResult> activationType)
    {
        ArgumentNullException.ThrowIfNull(activationType);

        return GetCachedType(type).ActivateType(activationType);
    }

    /// <summary>
    /// Invokes the supplied activation strategy with the run-time type and one argument.
    /// </summary>
    /// <typeparam name="TResult">The value returned by the activation strategy.</typeparam>
    /// <typeparam name="T1">The type of the strategy argument.</typeparam>
    /// <param name="type">The closed reference type to activate.</param>
    /// <param name="activationType">The strategy that performs the type-specific operation.</param>
    /// <param name="arg1">The argument supplied to the strategy.</param>
    /// <returns>The value returned by <paramref name="activationType"/>.</returns>
    public static TResult Activate<TResult, T1>(Type type, IActivationType<TResult, T1> activationType, T1 arg1)
    {
        ArgumentNullException.ThrowIfNull(activationType);

        return GetCachedType(type).ActivateType(activationType, arg1);
    }

    /// <summary>
    /// Invokes the supplied activation strategy with the run-time type and two arguments.
    /// </summary>
    /// <typeparam name="TResult">The value returned by the activation strategy.</typeparam>
    /// <typeparam name="T1">The type of the first strategy argument.</typeparam>
    /// <typeparam name="T2">The type of the second strategy argument.</typeparam>
    /// <param name="type">The closed reference type to activate.</param>
    /// <param name="activationType">The strategy that performs the type-specific operation.</param>
    /// <param name="arg1">The first argument supplied to the strategy.</param>
    /// <param name="arg2">The second argument supplied to the strategy.</param>
    /// <returns>The value returned by <paramref name="activationType"/>.</returns>
    public static TResult Activate<TResult, T1, T2>(Type type, IActivationType<TResult, T1, T2> activationType, T1 arg1, T2 arg2)
    {
        ArgumentNullException.ThrowIfNull(activationType);

        return GetCachedType(type).ActivateType(activationType, arg1, arg2);
    }

    static CachedType GetCachedType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.ContainsGenericParameters)
            throw new ArgumentException("The activation type must not contain unbound generic parameters.", nameof(type));

        if (type.IsValueType || type.IsByRef || type.IsPointer || type.IsFunctionPointer || type == typeof(void))
            throw new ArgumentException("The activation type must be a reference type.", nameof(type));

        return CachedTypes.GetValue(type, static value =>
            (CachedType)(Activator.CreateInstance(typeof(TypeAdapter<>).MakeGenericType(value))
                ?? throw new InvalidOperationException($"Unable to create an activation adapter for '{value}'.")));
    }


    interface CachedType
    {
        TResult ActivateType<TResult>(IActivationType<TResult> activationType);
        TResult ActivateType<TResult, T1>(IActivationType<TResult, T1> activationType, T1 arg1);
        TResult ActivateType<TResult, T1, T2>(IActivationType<TResult, T1, T2> activationType, T1 arg1, T2 arg2);
    }


    sealed class TypeAdapter<TAdapter> :
        CachedType
        where TAdapter : class
    {
        public TResult ActivateType<TResult>(IActivationType<TResult> activationType)
        {
            return activationType.ActivateType<TAdapter>();
        }

        public TResult ActivateType<TResult, T1>(IActivationType<TResult, T1> activationType, T1 arg1)
        {
            return activationType.ActivateType<TAdapter>(arg1);
        }

        public TResult ActivateType<TResult, T1, T2>(IActivationType<TResult, T1, T2> activationType, T1 arg1, T2 arg2)
        {
            return activationType.ActivateType<TAdapter>(arg1, arg2);
        }
    }
}
