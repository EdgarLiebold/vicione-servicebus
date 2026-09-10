namespace ViciOne.ServiceBus.Testing;

/// <summary>Evaluates whether an observed context satisfies a test query.</summary>
/// <typeparam name="TContext">The observed context type.</typeparam>
/// <param name="context">The context to evaluate.</param>
/// <returns><see langword="true"/> when the context matches; otherwise, <see langword="false"/>.</returns>
public delegate bool FilterDelegate<in TContext>(TContext context)
    where TContext : class;
