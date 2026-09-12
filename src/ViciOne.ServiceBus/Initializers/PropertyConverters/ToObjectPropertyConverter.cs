using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Exposes a typed input value as an object.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
internal sealed class ToObjectPropertyConverter<TInput> :
    IPropertyConverter<object, TInput>
{
    /// <inheritdoc />
    public Task<object?> ConvertAsync<T>(InitializeContext<T> context, TInput? input, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<object?>(cancellationToken);

        return Task.FromResult<object?>(input);
    }
}
