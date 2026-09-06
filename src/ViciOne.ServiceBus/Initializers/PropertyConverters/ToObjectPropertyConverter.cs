using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Converts to object property values.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
public class ToObjectPropertyConverter<TInput> :
    IPropertyConverter<object, TInput>
{
    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public Task<object?> ConvertAsync<T>(InitializeContext<T> context, TInput? input, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<object?>(cancellationToken); return Task.FromResult<object?>(input);
    }
}
