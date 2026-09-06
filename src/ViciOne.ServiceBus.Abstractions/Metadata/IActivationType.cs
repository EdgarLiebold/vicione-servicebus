namespace ViciOne.ServiceBus.Metadata;

/// <summary>Defines the operations required by activation type.</summary>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
public interface IActivationType<out TResult>
{
    /// <summary>Activates type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The t result produced by the operation.</returns>
    TResult ActivateType<T>()
        where T : class;
}


/// <summary>Defines the operations required by activation type.</summary>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
/// <typeparam name="T1">The 1 type.</typeparam>
public interface IActivationType<out TResult, in T1>
{
    /// <summary>Activates type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="arg1">The arg1.</param>
    /// <returns>The t result produced by the operation.</returns>
    TResult ActivateType<T>(T1 arg1)
        where T : class;
}


/// <summary>Defines the operations required by activation type.</summary>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
/// <typeparam name="T1">The 1 type.</typeparam>
/// <typeparam name="T2">The 2 type.</typeparam>
public interface IActivationType<out TResult, in T1, in T2>
{
    /// <summary>Activates type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="arg1">The arg1.</param>
    /// <param name="arg2">The arg2.</param>
    /// <returns>The t result produced by the operation.</returns>
    TResult ActivateType<T>(T1 arg1, T2 arg2)
        where T : class;
}
