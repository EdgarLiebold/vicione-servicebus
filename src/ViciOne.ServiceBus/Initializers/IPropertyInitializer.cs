using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>Populates message properties from a typed input object.</summary>
/// <typeparam name="TMessage">The message contract being populated.</typeparam>
/// <typeparam name="TInput">The input-object type.</typeparam>
public interface IPropertyInitializer<in TMessage, in TInput>
    where TMessage : class
    where TInput : class
{
    /// <summary>Applies this initializer to the message in <paramref name="context"/>.</summary>
    /// <param name="context">The message and input object used by the initializer.</param>
    /// <param name="cancellationToken">The token that cancels property initialization.</param>
    /// <returns>A task that completes after the message property has been populated.</returns>
    Task ApplyAsync(InitializeContext<TMessage, TInput> context, CancellationToken cancellationToken = default);
}


/// <summary>Populates message properties without requiring an input object.</summary>
/// <typeparam name="TMessage">The message contract being populated.</typeparam>
public interface IPropertyInitializer<in TMessage>
    where TMessage : class
{
    /// <summary>Applies this initializer to the message in <paramref name="context"/>.</summary>
    /// <param name="context">The message being populated.</param>
    /// <param name="cancellationToken">The token that cancels property initialization.</param>
    /// <returns>A task that completes after the message property has been populated.</returns>
    Task ApplyAsync(InitializeContext<TMessage> context, CancellationToken cancellationToken = default);
}
