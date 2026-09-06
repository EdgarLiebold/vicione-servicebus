namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Represents the method that handles filter context provider.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TSplit">The split type.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate TInput FilterContextProvider<out TInput, in TSplit>(TSplit context)
    where TSplit : class, PipeContext
    where TInput : class, PipeContext;
