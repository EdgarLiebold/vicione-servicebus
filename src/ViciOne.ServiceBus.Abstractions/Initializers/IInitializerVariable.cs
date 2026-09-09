using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>Resolves a message-property value from the current initialization context.</summary>
/// <typeparam name="T">The resolved value type.</typeparam>
public interface IInitializerVariable<T>
{
    /// <summary>Resolves the value for a message being initialized.</summary>
    /// <typeparam name="TMessage">The message contract being initialized.</typeparam>
    /// <param name="context">The message, input graph, and pipeline state available to the resolver.</param>
    /// <param name="cancellationToken">The token that cancels value resolution.</param>
    /// <returns>A task containing the resolved property value.</returns>
    Task<T> GetValueAsync<TMessage>(InitializeContext<TMessage> context, CancellationToken cancellationToken = default)
        where TMessage : class;
}
