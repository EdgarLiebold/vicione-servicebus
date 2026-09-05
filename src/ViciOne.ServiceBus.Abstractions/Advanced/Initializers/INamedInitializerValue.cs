namespace ViciOne.ServiceBus.Advanced.Initializers;

/// <summary>
/// Exposes the stable text representation of a value to the initializer conversion pipeline.
/// </summary>
public interface INamedInitializerValue
{
    /// <summary>
    /// Gets the stable value name.
    /// </summary>
    string Name { get; }
}

/// <summary>
/// Associates a named initializer value with its owning context type.
/// </summary>
/// <typeparam name="TContext">The owning context type.</typeparam>
public interface INamedInitializerValue<TContext> :
    INamedInitializerValue
    where TContext : class;
