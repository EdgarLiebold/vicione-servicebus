using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>Converts a property value using the current message-initialization context.</summary>
/// <typeparam name="TResult">The converted property type.</typeparam>
/// <typeparam name="TProperty">The source property type.</typeparam>
public interface IPropertyConverter<TResult, in TProperty>
{
    /// <summary>Converts a source property value for the message being initialized.</summary>
    /// <typeparam name="T">The message contract being initialized.</typeparam>
    /// <param name="context">The message graph and pipeline state available to the converter.</param>
    /// <param name="input">The source property value.</param>
    /// <param name="cancellationToken">The token that cancels conversion.</param>
    /// <returns>A task containing the converted property value.</returns>
    Task<TResult?> ConvertAsync<T>(InitializeContext<T> context, TProperty? input, CancellationToken cancellationToken = default)
        where T : class;
}
