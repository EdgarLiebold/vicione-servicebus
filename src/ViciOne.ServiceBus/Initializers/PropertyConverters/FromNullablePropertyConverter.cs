using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Returns a nullable value type or its default value.</summary>
/// <typeparam name="TResult">The underlying value type.</typeparam>
internal sealed class FromNullablePropertyConverter<TResult> :
    IPropertyConverter<TResult, TResult?>
    where TResult : struct
{
    Task<TResult> IPropertyConverter<TResult, TResult?>.ConvertAsync<T>(InitializeContext<T> context, TResult? input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<TResult>(cancellationToken);

        return Task.FromResult(input ?? default);
    }
}


/// <summary>Converts a nullable value through a converter for its underlying type.</summary>
/// <typeparam name="TResult">The converted property value type.</typeparam>
/// <typeparam name="TInput">The nullable source value type.</typeparam>
internal sealed class FromNullablePropertyConverter<TResult, TInput> :
    IPropertyConverter<TResult, TInput?>
    where TInput : struct
{
    readonly IPropertyConverter<TResult, TInput> _converter;

    /// <summary>Creates a nullable-input wrapper around <paramref name="converter"/>.</summary>
    /// <param name="converter">The converter applied to the underlying value.</param>
    public FromNullablePropertyConverter(IPropertyConverter<TResult, TInput> converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    Task<TResult?> IPropertyConverter<TResult, TInput?>.ConvertAsync<T>(InitializeContext<T> context, TInput? input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<TResult?>(cancellationToken);

        return _converter.ConvertAsync(context, input ?? default, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The property converter returned a null task.");
    }
}
