namespace ViciOne.ServiceBus.Metadata;

/// <summary>
/// Defines the contract for activation type.
/// </summary>
/// <typeparam name="TResult">The t result type.</typeparam>
public interface IActivationType<out TResult>
{
    /// <summary>
    /// Performs the activate type operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    TResult ActivateType<T>()
        where T : class;
}


/// <summary>
/// Defines the contract for activation type.
/// </summary>
/// <typeparam name="TResult">The t result type.</typeparam>
/// <typeparam name="T1">The t1 type.</typeparam>
public interface IActivationType<out TResult, in T1>
{
    /// <summary>
    /// Performs the activate type operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="arg1">The arg1 value.</param>
    /// <returns>The result of the operation.</returns>
    TResult ActivateType<T>(T1 arg1)
        where T : class;
}


/// <summary>
/// Defines the contract for activation type.
/// </summary>
/// <typeparam name="TResult">The t result type.</typeparam>
/// <typeparam name="T1">The t1 type.</typeparam>
/// <typeparam name="T2">The t2 type.</typeparam>
public interface IActivationType<out TResult, in T1, in T2>
{
    /// <summary>
    /// Performs the activate type operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="arg1">The arg1 value.</param>
    /// <param name="arg2">The arg2 value.</param>
    /// <returns>The result of the operation.</returns>
    TResult ActivateType<T>(T1 arg1, T2 arg2)
        where T : class;
}
