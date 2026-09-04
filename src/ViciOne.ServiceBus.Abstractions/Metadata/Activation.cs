using System;
using System.Collections.Concurrent;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>
/// Provides an activation implementation.
/// </summary>
public static class Activation
{
    /// <summary>
    /// Performs the activate operation.
    /// </summary>
    /// <typeparam name="TResult">The t result type.</typeparam>
    /// <param name="type">The type value.</param>
    /// <param name="activationType">The activation type value.</param>
    /// <returns>The result of the operation.</returns>
    public static TResult Activate<TResult>(Type type, IActivationType<TResult> activationType)
    {
        return Cached.Instance.Value
            .GetOrAdd(type,
                _ => (CachedType)(Activator.CreateInstance(typeof(TypeAdapter<>).MakeGenericType(type)) ?? throw new InvalidOperationException()))
            .ActivateType(activationType);
    }

    /// <summary>
    /// Performs the activate operation.
    /// </summary>
    /// <typeparam name="TResult">The t result type.</typeparam>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <param name="type">The type value.</param>
    /// <param name="activationType">The activation type value.</param>
    /// <param name="arg1">The arg1 value.</param>
    /// <returns>The result of the operation.</returns>
    public static TResult Activate<TResult, T1>(Type type, IActivationType<TResult, T1> activationType, T1 arg1)
    {
        return Cached.Instance.Value
            .GetOrAdd(type,
                _ => (CachedType)(Activator.CreateInstance(typeof(TypeAdapter<>).MakeGenericType(type)) ?? throw new InvalidOperationException()))
            .ActivateType(activationType, arg1);
    }

    /// <summary>
    /// Performs the activate operation.
    /// </summary>
    /// <typeparam name="TResult">The t result type.</typeparam>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <typeparam name="T2">The t2 type.</typeparam>
    /// <param name="type">The type value.</param>
    /// <param name="activationType">The activation type value.</param>
    /// <param name="arg1">The arg1 value.</param>
    /// <param name="arg2">The arg2 value.</param>
    /// <returns>The result of the operation.</returns>
    public static TResult Activate<TResult, T1, T2>(Type type, IActivationType<TResult, T1, T2> activationType, T1 arg1, T2 arg2)
    {
        return Cached.Instance.Value
            .GetOrAdd(type,
                _ => (CachedType)(Activator.CreateInstance(typeof(TypeAdapter<>).MakeGenericType(type)) ?? throw new InvalidOperationException()))
            .ActivateType(activationType, arg1, arg2);
    }


    interface CachedType
    {
        Type Type { get; }
        TResult ActivateType<TResult>(IActivationType<TResult> activationType);
        TResult ActivateType<TResult, T1>(IActivationType<TResult, T1> activationType, T1 arg1);
        TResult ActivateType<TResult, T1, T2>(IActivationType<TResult, T1, T2> activationType, T1 arg1, T2 arg2);
    }


    static class Cached
    {
        internal static readonly Lazy<ConcurrentDictionary<Type, CachedType>> Instance = new(() => new ConcurrentDictionary<Type, CachedType>());
    }


    class TypeAdapter<TAdapter> :
        CachedType
        where TAdapter : class
    {
        public Type Type => typeof(TAdapter);

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
