using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Wraps a value-type property in its nullable form.</summary>
/// <typeparam name="TResult">The underlying value type.</typeparam>
internal sealed class ToNullablePropertyConverter<TResult> :
    IPropertyConverter<TResult?, TResult>
    where TResult : struct
{
    /// <inheritdoc />
    public Task<TResult?> ConvertAsync<T>(InitializeContext<T> context, TResult input, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<TResult?>(cancellationToken);

        return Task.FromResult<TResult?>(input);
    }
}


/// <summary>Converts a property and wraps the result in its nullable form.</summary>
/// <typeparam name="TResult">The underlying result value type.</typeparam>
/// <typeparam name="TInput">The source value type.</typeparam>
/// <remarks>An accepted underlying conversion is observed to its original terminal outcome.</remarks>
internal sealed class ToNullablePropertyConverter<TResult, TInput> :
    IPropertyConverter<TResult?, TInput>
    where TResult : struct
{
    readonly IPropertyConverter<TResult, TInput> _converter;

    /// <summary>Creates a nullable wrapper around <paramref name="converter"/>.</summary>
    /// <param name="converter">The converter that produces the underlying value.</param>
    public ToNullablePropertyConverter(IPropertyConverter<TResult, TInput> converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    /// <inheritdoc />
    public async Task<TResult?> ConvertAsync<T>(InitializeContext<T> context, TInput? input, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        Task<TResult> resultTask = _converter.ConvertAsync(context, input, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The property converter returned null.");
        return await resultTask.ConfigureAwait(false);
    }
}
