namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>
/// Represents the method that handles merge filter context provider.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
/// <typeparam name="TSplit">The t split type.</typeparam>
/// <param name="inputContext">The input context value.</param>
/// <param name="context">The operation context.</param>
/// <returns>The result of the operation.</returns>
public delegate TInput MergeFilterContextProvider<TInput, in TSplit>(TInput inputContext, TSplit context)
    where TSplit : class, PipeContext
    where TInput : class, PipeContext;
