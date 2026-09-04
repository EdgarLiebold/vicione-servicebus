namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>
/// Represents the method that handles filter context provider.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
/// <typeparam name="TSplit">The t split type.</typeparam>
/// <param name="context">The operation context.</param>
/// <returns>The result of the operation.</returns>
public delegate TInput FilterContextProvider<out TInput, in TSplit>(TSplit context)
    where TSplit : class, PipeContext
    where TInput : class, PipeContext;
