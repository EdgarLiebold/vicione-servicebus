namespace ViciOne.ServiceBus.Metadata;

/// <summary>
/// Defines an operation that is invoked with a run-time-selected reference type.
/// </summary>
/// <typeparam name="TResult">The metadata or adapter created for the selected type.</typeparam>
internal interface IActivationType<out TResult>
{
    /// <summary>
    /// Performs the operation for the selected reference type.
    /// </summary>
    /// <typeparam name="T">The selected reference type.</typeparam>
    /// <returns>The metadata or adapter created for <typeparamref name="T"/>.</returns>
    TResult ActivateType<T>()
        where T : class;
}


/// <summary>
/// Defines an operation that is invoked with a run-time-selected reference type and one argument.
/// </summary>
/// <typeparam name="TResult">The metadata or adapter created for the selected type.</typeparam>
/// <typeparam name="T1">The context type used while creating the selected-type result.</typeparam>
internal interface IActivationType<out TResult, in T1>
{
    /// <summary>
    /// Performs the operation for the selected reference type.
    /// </summary>
    /// <typeparam name="T">The selected reference type.</typeparam>
    /// <param name="arg1">The context used to create the selected-type result.</param>
    /// <returns>The metadata or adapter created for <typeparamref name="T"/>.</returns>
    TResult ActivateType<T>(T1 arg1)
        where T : class;
}


/// <summary>
/// Defines an operation that is invoked with a run-time-selected reference type and two arguments.
/// </summary>
/// <typeparam name="TResult">The metadata or adapter created for the selected type.</typeparam>
/// <typeparam name="T1">The first context type used while creating the selected-type result.</typeparam>
/// <typeparam name="T2">The second context type used while creating the selected-type result.</typeparam>
internal interface IActivationType<out TResult, in T1, in T2>
{
    /// <summary>
    /// Performs the operation for the selected reference type.
    /// </summary>
    /// <typeparam name="T">The selected reference type.</typeparam>
    /// <param name="arg1">The first context used to create the selected-type result.</param>
    /// <param name="arg2">The second context used to create the selected-type result.</param>
    /// <returns>The metadata or adapter created for <typeparamref name="T"/>.</returns>
    TResult ActivateType<T>(T1 arg1, T2 arg2)
        where T : class;
}
