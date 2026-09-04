using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>
/// Provides a to object property converter implementation.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
public class ToObjectPropertyConverter<TInput> :
    IPropertyConverter<object, TInput>
{
    /// <summary>
    /// Performs the convert operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="input">The input value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<object?> ConvertAsync<T>(InitializeContext<T> context, TInput? input, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<object?>(cancellationToken); return Task.FromResult<object?>(input);
    }
}
