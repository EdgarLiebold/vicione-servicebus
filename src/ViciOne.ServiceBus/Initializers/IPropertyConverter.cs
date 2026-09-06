using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>A message property converter, which is async, and has access to the context.</summary>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public interface IPropertyConverter<TResult, in TProperty>
{
    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    Task<TResult?> ConvertAsync<T>(InitializeContext<T> context, TProperty? input, CancellationToken cancellationToken = default)
        where T : class;
}
