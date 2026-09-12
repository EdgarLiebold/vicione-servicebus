using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>Resolves a message-property value from an initialization input.</summary>
/// <typeparam name="TInput">The input-object type.</typeparam>
/// <typeparam name="TProperty">The resolved property type.</typeparam>
public interface IPropertyProvider<in TInput, TProperty>
    where TInput : class
{
    /// <summary>Resolves the property value for the current message and input object.</summary>
    /// <typeparam name="T">The message contract being initialized.</typeparam>
    /// <param name="context">The message, input graph, and pipeline state available to the provider.</param>
    /// <param name="cancellationToken">The token that cancels value resolution.</param>
    /// <returns>A task containing the resolved property value.</returns>
    Task<TProperty?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class;
}
